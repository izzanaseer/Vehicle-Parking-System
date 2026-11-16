using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Moq;
using NUnit.Framework;
using ParkingSystem.Models;        // adjust
using ParkingSystem.Tests.Infrastructure;

namespace ParkingSystem.Tests.Endpoints;

[TestFixture]
public class ParkingEndpointTests
{
    private ParkingApiFactory _factory = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp() => _factory = new ParkingApiFactory();

    [OneTimeTearDown]
    public void OneTimeTearDown() => _factory.Dispose();

    [SetUp]
    public void SetUp() => _factory.ResetMocks();

    private static ParkingTicket MakeTicket() => new()
    {
        Id = 1,
        Vehicle = new Vehicle { Id = 1, VehicleNumber = "ABC-123", Type = VehicleType.Car },
        Slot = new ParkingSlot { Id = 1, SlotNumber = "C1", SlotType = VehicleType.Car, IsOccupied = true },
        EntryTime = DateTime.UtcNow.AddHours(-2),
        IsPaid = false
    };

    // ---------- Security rules ----------

    [Test]
    public async Task GetAvailability_NoToken_Returns401()
    {
        var client = _factory.CreateClientAs(null);

        var response = await client.GetAsync("/api/parking/availability");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task GetTicket_UserRole_Returns403AndNeverReachesRepository()
    {
        var client = _factory.CreateClientAs("User");

        var response = await client.GetAsync("/api/parking/tickets/1");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        _factory.Repo.Verify(r => r.GetTicketWithDetailsAsync(It.IsAny<int>()), Times.Never);
    }

    // ---------- GetAvailability ----------

    [Test]
    public async Task GetAvailability_LoggedInUser_ReturnsCountsPerVehicleType()
    {
        _factory.Tracker.Setup(t => t.GetTotal(VehicleType.Car)).Returns(3);
        _factory.Tracker.Setup(t => t.GetAvailable(VehicleType.Car)).Returns(2);
        var client = _factory.CreateClientAs("User");

        var response = await client.GetAsync("/api/parking/availability");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var car = json.EnumerateArray()
            .First(e => e.GetProperty("vehicleType").GetString() == "Car");
        Assert.That(car.GetProperty("total").GetInt32(), Is.EqualTo(3));
        Assert.That(car.GetProperty("available").GetInt32(), Is.EqualTo(2));
    }

    // ---------- GetTicket ----------

    [Test]
    public async Task GetTicket_TicketDoesNotExist_Returns404()
    {
        _factory.Repo.Setup(r => r.GetTicketWithDetailsAsync(99))
                     .ReturnsAsync((ParkingTicket?)null);
        var client = _factory.CreateClientAs("Admin");

        var response = await client.GetAsync("/api/parking/tickets/99");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task GetTicket_TicketExists_Returns200WithTicketDetails()
    {
        _factory.Repo.Setup(r => r.GetTicketWithDetailsAsync(1)).ReturnsAsync(MakeTicket());
        var client = _factory.CreateClientAs("Admin");

        var response = await client.GetAsync("/api/parking/tickets/1");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(json.GetProperty("ticketId").GetInt32(), Is.EqualTo(1));
        Assert.That(json.GetProperty("vehicleNumber").GetString(), Is.EqualTo("ABC-123"));
        Assert.That(json.GetProperty("slotNumber").GetString(), Is.EqualTo("C1"));
        Assert.That(json.GetProperty("isPaid").GetBoolean(), Is.False);
    }
}