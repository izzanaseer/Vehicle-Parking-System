using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ParkingSystem.Data;
using ParkingSystem.Models;

namespace ParkingSystem.Repositories;

public class ParkingRepository(ParkingDbContext db) : IParkingRepository
{
    public Task<Vehicle?> GetVehicleByNumberAsync(string vehicleNumber) =>
        db.Vehicles.FirstOrDefaultAsync(v => v.VehicleNumber == vehicleNumber);

    public Task<bool> HasOpenTicketAsync(int vehicleId) =>
        db.ParkingTickets.AnyAsync(t => t.VehicleId == vehicleId && t.ExitTime == null);

    public Task<ParkingSlot?> GetFreeSlotAsync(VehicleType type) =>
        db.ParkingSlots.FirstOrDefaultAsync(s => s.SlotType == type && !s.IsOccupied);

    public Task<ParkingTicket?> GetTicketWithVehicleAsync(int id) =>
        db.ParkingTickets
            .Include(t => t.Vehicle)
            .FirstOrDefaultAsync(t => t.Id == id);

    public Task<ParkingTicket?> GetTicketWithDetailsAsync(int id) =>
        db.ParkingTickets
            .Include(t => t.Vehicle)
            .Include(t => t.Slot)
            .FirstOrDefaultAsync(t => t.Id == id);

    public Task<VehicleRate?> GetRateAsync(VehicleType type) =>
        db.VehicleRates.FirstOrDefaultAsync(r => r.Type == type);

    public void AddVehicle(Vehicle vehicle) => db.Vehicles.Add(vehicle);

    public void AddTicket(ParkingTicket ticket) => db.ParkingTickets.Add(ticket);

    public Task SaveChangesAsync() => db.SaveChangesAsync();

    public async Task<IParkingTransaction> BeginSerializableTransactionAsync()
    {
        var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        return new EfParkingTransaction(tx);
    }
}

// Wraps EF's transaction so the controller never needs to know about EF types.
public class EfParkingTransaction(IDbContextTransaction tx) : IParkingTransaction
{
    public Task CommitAsync() => tx.CommitAsync();
    public ValueTask DisposeAsync() => tx.DisposeAsync();
}