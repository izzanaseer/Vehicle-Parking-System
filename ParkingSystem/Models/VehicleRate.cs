namespace ParkingSystem.Models;

public class VehicleRate
{
    public int Id { get; set; }
    public VehicleType Type { get; set; }
    public decimal RatePerHour { get; set; }
}