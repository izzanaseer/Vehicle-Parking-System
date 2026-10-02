namespace ParkingSystem.Models;

public class Vehicle
{
    public int Id { get; set; }
    public string VehicleNumber { get; set; } = string.Empty;
    public VehicleType Type { get; set; }
}