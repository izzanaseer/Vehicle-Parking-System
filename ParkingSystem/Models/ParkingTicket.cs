namespace ParkingSystem.Models;

public class ParkingTicket
{
    public int Id { get; set; }

    public int VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = null!;

    public int SlotId { get; set; }
    public ParkingSlot Slot { get; set; } = null!;

    public DateTime EntryTime { get; set; }
    public DateTime? ExitTime { get; set; }

    public decimal? FeeAmount { get; set; }
    public string? PaymentMethod { get; set; }
    public bool IsPaid { get; set; }
}