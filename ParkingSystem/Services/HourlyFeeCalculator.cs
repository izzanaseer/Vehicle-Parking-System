using ParkingSystem.Models;

namespace ParkingSystem.Services;

public class HourlyFeeCalculator : IFeeCalculator
{
    public decimal CalculateFee(VehicleType type, DateTime entryTime, DateTime exitTime, decimal ratePerHour)
    {
        var duration = exitTime - entryTime;
        var hours = Math.Ceiling(duration.TotalHours);
        if (hours < 1)
        {
            hours = 1;
        }

        return (decimal)hours * ratePerHour;
    }
}