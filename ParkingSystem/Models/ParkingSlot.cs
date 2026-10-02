namespace ParkingSystem.Models;

public class ParkingSlot
{
    public int Id { get; set; }
    public string SlotNumber { get; set; } = string.Empty;
    public VehicleType SlotType { get; set; }
    public bool IsOccupied { get; set; }
}