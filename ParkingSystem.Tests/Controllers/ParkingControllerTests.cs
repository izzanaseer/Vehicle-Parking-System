using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using ParkingSystem.Controllers;   // adjust
using ParkingSystem.Models;        // adjust
using ParkingSystem.Repositories;
using ParkingSystem.Services;      // adjust

namespace ParkingSystem.Tests.Controllers;

[TestFixture]
public class ParkingControllerTests
{
    private Mock<IParkingRepository> _repo = null!;
    private Mock<IFeeCalculator> _fee = null!;
    private Mock<IParkingSpaceTracker> _tracker = null!;
    private ParkingController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IParkingRepository>();
        _fee = new Mock<IFeeCalculator>();
        _tracker = new Mock<IParkingSpaceTracker>();
        _controller = new ParkingController(_repo.Object, _fee.Object, _tracker.Object);
    }

    // ---------- helpers ----------

    // The controller returns anonymous objects, so we read their properties by name.
    private static object? Prop(object? obj, string name) =>
        obj!.GetType().GetProperty(name)!.GetValue(obj);

    private static ParkingTicket MakeTicket(VehicleType type = VehicleType.Car) => new()
    {
        Id = 1,
        Vehicle = new Vehicle { Id = 1, VehicleNumber = "ABC-123", Type = type },
        Slot = new ParkingSlot { Id = 1, SlotNumber = "C1", SlotType = type, IsOccupied = true },
        EntryTime = DateTime.UtcNow.AddHours(-2),
        IsPaid = false
    };

    // ---------- CalculateExit ----------

    [Test]
    public async Task CalculateExit_ValidTicket_SetsExitTimeAndFeeAndSaves()
    {
        var ticket = MakeTicket();
        _repo.Setup(r => r.GetTicketWithVehicleAsync(1)).ReturnsAsync(ticket);
        _repo.Setup(r => r.GetRateAsync(VehicleType.Car))
             .ReturnsAsync(new VehicleRate { Type = VehicleType.Car, RatePerHour = 50m });
        _fee.Setup(f => f.CalculateFee(
                VehicleType.Car, ticket.EntryTime, It.IsAny<DateTime>(), 50m))
            .Returns(150m);

        var result = await _controller.CalculateExit(1);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var body = ((OkObjectResult)result).Value;
        Assert.That(Prop(body, "feeAmount"), Is.EqualTo(150m));
        Assert.That(ticket.ExitTime, Is.Not.Null);
        Assert.That(ticket.FeeAmount, Is.EqualTo(150m));
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public void CalculateExit_SaveFails_ExceptionIsNotSwallowed()
    {
        var ticket = MakeTicket();
        _repo.Setup(r => r.GetTicketWithVehicleAsync(1)).ReturnsAsync(ticket);
        _repo.Setup(r => r.GetRateAsync(VehicleType.Car))
             .ReturnsAsync(new VehicleRate { Type = VehicleType.Car, RatePerHour = 50m });
        _fee.Setup(f => f.CalculateFee(
                It.IsAny<VehicleType>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<decimal>()))
            .Returns(100m);
        _repo.Setup(r => r.SaveChangesAsync())
             .ThrowsAsync(new DbUpdateException("Database failure"));

        Assert.ThrowsAsync<DbUpdateException>(() => _controller.CalculateExit(1));
    }
}