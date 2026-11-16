using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using ParkingSystem.Models;
using ParkingSystem.Repositories;
using ParkingSystem.Tests.Infrastructure;

namespace ParkingSystem.Tests.Endpoints;

[TestFixture]
public class IssueTicketEndpointTests
{
    private const string Url = "/api/parking/tickets";
    private ParkingApiFactory _factory = null!;
    private Mock<IParkingTransaction> _tx = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp() => _factory = new ParkingApiFactory();

    [OneTimeTearDown]
    public void OneTimeTearDown() => _factory.Dispose();

    [SetUp]
    public void SetUp()
    {
        _factory.ResetMocks();

        _tx = new Mock<IParkingTransaction>();
        _tx.Setup(t => t.CommitAsync()).Returns(Task.CompletedTask);
        _tx.Setup(t => t.DisposeAsync()).Returns(ValueTask.CompletedTask);
        _factory.Repo.Setup(r => r.BeginSerializableTransactionAsync())
                     .ReturnsAsync(_tx.Object);
    }

    // ---------- helpers ----------

    private static object Body(string number = "ABC-123", string type = "Car") =>
        new { vehicleNumber = number, vehicleType = type };

    private static ParkingSlot FreeCarSlot() =>
        new() { Id = 1, SlotNumber = "C1", SlotType = VehicleType.Car, IsOccupied = false };

    private static Vehicle ExistingVehicle(VehicleType type) =>
        new() { Id = 5, VehicleNumber = "ABC-123", Type = type };

    // new vehicle + a free car slot: the "everything works" setup
    private ParkingSlot SetupHappyPath()
    {
        var slot = FreeCarSlot();
        _factory.Repo.Setup(r => r.GetVehicleByNumberAsync("ABC-123")).ReturnsAsync((Vehicle?)null);
        _factory.Repo.Setup(r => r.GetFreeSlotAsync(VehicleType.Car)).ReturnsAsync(slot);
        return slot;
    }

    private void VerifyNothingWasCommitted()
    {
        _tx.Verify(t => t.CommitAsync(), Times.Never);
        _factory.Repo.Verify(r => r.AddTicket(It.IsAny<ParkingTicket>()), Times.Never);
        _factory.Tracker.Verify(t => t.TryOccupySlot(It.IsAny<VehicleType>()), Times.Never);
    }

    // ---------- security ----------

    [Test]
    public async Task IssueTicket_UserRole_Returns403AndTouchesNothing()
    {
        var response = await _factory.CreateClientAs("User").PostAsJsonAsync(Url, Body());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        _factory.Repo.Verify(r => r.GetVehicleByNumberAsync(It.IsAny<string>()), Times.Never);
    }

    // ---------- validation ----------

    [TestCase("")]
    [TestCase("   ")]
    public async Task IssueTicket_BlankVehicleNumber_Returns400(string number)
    {
        var response = await _factory.CreateClientAs("Admin").PostAsJsonAsync(Url, Body(number));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        _factory.Repo.Verify(r => r.GetVehicleByNumberAsync(It.IsAny<string>()), Times.Never);
    }

    // ---------- success ----------

    [Test]
    public async Task IssueTicket_NewVehicleAndFreeSlot_Returns200AndSavesEverything()
    {
        var slot = SetupHappyPath();

        var response = await _factory.CreateClientAs("Admin")
            .PostAsJsonAsync(Url, Body("  abc-123 "));   // messy input on purpose

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(json.GetProperty("vehicleNumber").GetString(), Is.EqualTo("ABC-123"));
        Assert.That(json.GetProperty("slotNumber").GetString(), Is.EqualTo("C1"));

        Assert.That(slot.IsOccupied, Is.True);
        _factory.Repo.Verify(r => r.GetVehicleByNumberAsync("ABC-123"), Times.Once);
        _factory.Repo.Verify(r => r.AddVehicle(It.Is<Vehicle>(
            v => v.VehicleNumber == "ABC-123" && v.Type == VehicleType.Car)), Times.Once);
        _factory.Repo.Verify(r => r.AddTicket(It.IsAny<ParkingTicket>()), Times.Once);
        _factory.Repo.Verify(r => r.SaveChangesAsync(), Times.Once);
        _tx.Verify(t => t.CommitAsync(), Times.Once);
        _factory.Tracker.Verify(t => t.TryOccupySlot(VehicleType.Car), Times.Once);
    }

    // ---------- business rule failures ----------

    [Test]
    public async Task IssueTicket_VehicleRegisteredAsDifferentType_Returns400()
    {
        _factory.Repo.Setup(r => r.GetVehicleByNumberAsync("ABC-123"))
                     .ReturnsAsync(ExistingVehicle(VehicleType.Bike));

        var response = await _factory.CreateClientAs("Admin").PostAsJsonAsync(Url, Body(type: "Car"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        _factory.Repo.Verify(r => r.GetFreeSlotAsync(It.IsAny<VehicleType>()), Times.Never);
        VerifyNothingWasCommitted();
    }

    [Test]
    public async Task IssueTicket_VehicleAlreadyParked_Returns409()
    {
        var vehicle = ExistingVehicle(VehicleType.Car);
        _factory.Repo.Setup(r => r.GetVehicleByNumberAsync("ABC-123")).ReturnsAsync(vehicle);
        _factory.Repo.Setup(r => r.HasOpenTicketAsync(vehicle.Id)).ReturnsAsync(true);

        var response = await _factory.CreateClientAs("Admin").PostAsJsonAsync(Url, Body());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        VerifyNothingWasCommitted();
    }

    [Test]
    public async Task IssueTicket_NoFreeSlot_Returns409AndSavesNothing()
    {
        _factory.Repo.Setup(r => r.GetVehicleByNumberAsync("ABC-123")).ReturnsAsync((Vehicle?)null);
        _factory.Repo.Setup(r => r.GetFreeSlotAsync(VehicleType.Car)).ReturnsAsync((ParkingSlot?)null);

        var response = await _factory.CreateClientAs("Admin").PostAsJsonAsync(Url, Body());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        _factory.Repo.Verify(r => r.SaveChangesAsync(), Times.Never);
        VerifyNothingWasCommitted();
    }

    // ---------- failures during save (the exception handler) ----------

    [Test]
    public async Task IssueTicket_ConcurrencyConflictOnSave_Returns409()
    {
        SetupHappyPath();
        _factory.Repo.Setup(r => r.SaveChangesAsync())
                     .ThrowsAsync(new DbUpdateConcurrencyException("conflict"));

        var response = await _factory.CreateClientAs("Admin").PostAsJsonAsync(Url, Body());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(problem.GetProperty("title").GetString(), Does.Contain("changed by another request"));
        _tx.Verify(t => t.CommitAsync(), Times.Never);
        _factory.Tracker.Verify(t => t.TryOccupySlot(It.IsAny<VehicleType>()), Times.Never);
    }

    [Test]
    public async Task IssueTicket_UnexpectedErrorOnSave_Returns500()
    {
        SetupHappyPath();
        _factory.Repo.Setup(r => r.SaveChangesAsync())
                     .ThrowsAsync(new InvalidOperationException("boom"));

        var response = await _factory.CreateClientAs("Admin").PostAsJsonAsync(Url, Body());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(problem.GetProperty("title").GetString(), Is.EqualTo("An unexpected error occurred."));
        _tx.Verify(t => t.CommitAsync(), Times.Never);
        _factory.Tracker.Verify(t => t.TryOccupySlot(It.IsAny<VehicleType>()), Times.Never);
    }
}