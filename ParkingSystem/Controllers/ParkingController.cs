using Microsoft.AspNetCore.Mvc;
using ParkingSystem.Services;

using Microsoft.AspNetCore.Authorization;

using Microsoft.EntityFrameworkCore;
using ParkingSystem.Data;
using ParkingSystem.Models;
using ParkingSystem.Dtos;

namespace ParkingSystem.Controllers;

[ApiController]                                         // attribute so that we turns on API behaviors
[Route("api/[controller]")]                             // sets the URL prefix for everything in this class. [controller] is a placeholder that becomes the class name e.g ParkingController becomes parking, so the prefix is api/parking.
public class ParkingController (ParkingDbContext db, IFeeCalculator feeCalculator) : ControllerBase
{
    [Authorize]
    [HttpGet("availability")]                           // answer GET requests at availability. URL becomes api/parking/availability.
    public IActionResult GetAvailability()
    {
        var tracker = ParkingSpaceTracker.Instance;

        var breakdown = Enum.GetValues<VehicleType>().Select(type => new
        {
            vehicleType = type.ToString(),
            total = tracker.GetTotal(type),
            available = tracker.GetAvailable(type)
        });

        return Ok(breakdown);
    }


    [Authorize(Roles = "Admin")]
    [HttpPost("tickets")]
    public async Task<IActionResult> IssueTicket([FromBody] IssueTicketRequest request)
    {
        var vehicleNumber = request.VehicleNumber?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(vehicleNumber))
        {
            return BadRequest("Vehicle number is required.");
        }

        // Find or create the vehicle
        var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.VehicleNumber == vehicleNumber);

        if (vehicle is null)
        {
            vehicle = new Vehicle { VehicleNumber = vehicleNumber, Type = request.VehicleType };
            db.Vehicles.Add(vehicle);
        }
        else
        {
            if (vehicle.Type != request.VehicleType)
            {
                return BadRequest($"Vehicle {vehicleNumber} is registered as {vehicle.Type}, not {request.VehicleType}.");
            }
            
            var alreadyParked = await db.ParkingTickets
                .AnyAsync(t => t.VehicleId == vehicle.Id && t.ExitTime == null);
            if (alreadyParked)
            {
                return Conflict($"Vehicle {vehicleNumber} is already parked and hasn't exited yet.");
            }
        }

        var slot = await db.ParkingSlots.FirstOrDefaultAsync(
            s => s.SlotType == request.VehicleType && !s.IsOccupied);

        if (slot is null)
        {
            return Conflict($"No available {request.VehicleType} slots.");
        }

        slot.IsOccupied = true;

        var ticket = new ParkingTicket
        {
            Vehicle = vehicle,
            Slot = slot,
            EntryTime = DateTime.UtcNow,
            IsPaid = false
        };

        db.ParkingTickets.Add(ticket);
        await db.SaveChangesAsync();

        ParkingSpaceTracker.Instance.TryOccupySlot(request.VehicleType);

        return Ok(new
        {
            ticketId = ticket.Id,
            vehicleNumber = vehicle.VehicleNumber,
            slotNumber = slot.SlotNumber,
            entryTime = ticket.EntryTime
        });
    }


    [Authorize(Roles = "Admin")]
    [HttpPatch("tickets/{id}/exit")]
    public async Task<IActionResult> CalculateExit(int id)
    {
        var ticket = await db.ParkingTickets
            .Include(t => t.Vehicle)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket is null)
        { 
            return NotFound("Ticket not found.");
        }
        
        if (ticket.ExitTime is not null)
        {
            return Conflict("Ticket already has an exit recorded.");
        } 

        var rate = await db.VehicleRates.FirstOrDefaultAsync(r => r.Type == ticket.Vehicle.Type);
        if (rate is null)
        {
            return StatusCode(500, "No rate configured for this vehicle type.");
        }

        ticket.ExitTime = DateTime.UtcNow;
        ticket.FeeAmount = feeCalculator.CalculateFee(
            ticket.Vehicle.Type, ticket.EntryTime, ticket.ExitTime.Value, rate.RatePerHour);

        await db.SaveChangesAsync();

        return Ok(new
        {
            ticketId = ticket.Id,
            vehicleNumber = ticket.Vehicle.VehicleNumber,
            entryTime = ticket.EntryTime,
            exitTime = ticket.ExitTime,
            feeAmount = ticket.FeeAmount
        });
    }
    
    [Authorize(Roles = "Admin")]
    [HttpGet("tickets/{id}")]
    public async Task<IActionResult> GetTicket(int id)
    {
        var ticket = await db.ParkingTickets
            .Include(t => t.Vehicle)
            .Include(t => t.Slot)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket is null) return NotFound("Ticket not found.");

        return Ok(new
        {
            ticketId = ticket.Id,
            vehicleNumber = ticket.Vehicle.VehicleNumber,
            slotNumber = ticket.Slot.SlotNumber,
            entryTime = ticket.EntryTime,
            exitTime = ticket.ExitTime,
            feeAmount = ticket.FeeAmount,
            isPaid = ticket.IsPaid
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("tickets/{id}/pay")]
    public async Task<IActionResult> PayTicket(int id, [FromBody] PaymentRequest request)
    {
        var ticket = await db.ParkingTickets
            .Include(t => t.Slot)
            .Include(t => t.Vehicle)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket is null) 
        {
            return NotFound("Ticket not found.");
        }

        if (ticket.ExitTime is null) 
        {
            return BadRequest("Calculate exit fee before recording payment.");
        }

        if (ticket.IsPaid) 
        {
            return Conflict("Ticket already paid.");
        }

        ticket.PaymentMethod = request.PaymentMethod;
        ticket.IsPaid = true;
        ticket.Slot.IsOccupied = false;

        await db.SaveChangesAsync();
        ParkingSpaceTracker.Instance.TryReleaseSlot(ticket.Vehicle.Type);

        return Ok(new
        {
            ticketId = ticket.Id,
            paymentMethod = ticket.PaymentMethod,
            feeAmount = ticket.FeeAmount,
            slotFreed = ticket.Slot.SlotNumber
        });
    }
    
}


/*
    The class name must end in Controller, or the framework won't recognize it.
    The : ControllerBase means it inherits from ControllerBase, which gives it ready-made helpers like Ok(), NotFound() and BadRequest()
*/