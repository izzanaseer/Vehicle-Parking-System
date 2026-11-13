using ParkingSystem.Models;

namespace ParkingSystem.Services;

public interface IParkingSpaceTracker
{
    int GetTotal(VehicleType type);
    int GetAvailable(VehicleType type);
    bool TryOccupySlot(VehicleType type);
    bool TryReleaseSlot(VehicleType type);
}