using ParkingSystem.Models;

namespace ParkingSystem.Repositories;

public interface IParkingRepository
{
    Task<Vehicle?> GetVehicleByNumberAsync(string vehicleNumber);
    Task<bool> HasOpenTicketAsync(int vehicleId);
    Task<ParkingSlot?> GetFreeSlotAsync(VehicleType type);
    Task<ParkingTicket?> GetTicketWithVehicleAsync(int id);
    Task<ParkingTicket?> GetTicketWithDetailsAsync(int id);   // includes Vehicle and Slot
    Task<VehicleRate?> GetRateAsync(VehicleType type);
    void AddVehicle(Vehicle vehicle);
    void AddTicket(ParkingTicket ticket);
    Task SaveChangesAsync();
    Task<IParkingTransaction> BeginSerializableTransactionAsync();
}