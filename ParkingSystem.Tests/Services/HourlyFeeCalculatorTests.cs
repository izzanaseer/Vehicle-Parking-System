using NUnit.Framework;
using ParkingSystem.Models;    // adjust: wherever VehicleType lives
using ParkingSystem.Services;  // adjust: wherever HourlyFeeCalculator lives

namespace ParkingSystem.Tests.Services;

[TestFixture]
public class HourlyFeeCalculatorTests
{
    private HourlyFeeCalculator _calculator = null!;
    private readonly DateTime _entry = new(2026, 11, 13, 10, 0, 0);

    [SetUp]
    public void SetUp()
    {
        _calculator = new HourlyFeeCalculator();
    }

    [TestCase(1, 1)]     // 1 minute -> charged 1 hour
    [TestCase(59, 1)]
    [TestCase(60, 1)]    // exactly 1 hour -> 1 hour
    [TestCase(61, 2)]    // 1 minute over -> rounds up to 2
    [TestCase(121, 3)]
    public void CalculateFee_DurationRoundsUpToFullHours(int minutes, int expectedHours)
    {
        // Arrange
        var exit = _entry.AddMinutes(minutes);

        // Act
        var fee = _calculator.CalculateFee(VehicleType.Car, _entry, exit, 100m);

        // Assert
        Assert.That(fee, Is.EqualTo(expectedHours * 100m));
    }

    [Test]
    public void CalculateFee_ZeroDuration_ChargesMinimumOneHour()
    {
        var fee = _calculator.CalculateFee(VehicleType.Car, _entry, _entry, 100m);

        Assert.That(fee, Is.EqualTo(100m));
    }

    [Test]
    public void CalculateFee_ExitBeforeEntry_CurrentBehaviourChargesMinimumOneHour()
    {
        var exit = _entry.AddMinutes(-30);

        var fee = _calculator.CalculateFee(VehicleType.Car, _entry, exit, 100m);

        Assert.That(fee, Is.EqualTo(100m));
    }
}