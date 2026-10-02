using ParkingSystem.Models;
namespace ParkingSystem.Services;

public class ParkingSpaceTracker
{
    /*
    The first line creates the one tracker object and stores it. The second, the private 
    constructor, stops anyone outside the class from creating another one with new. The 
    third, Instance, is how everyone else gets that same single object. So the whole app 
    shares one available-slot count.
    */
    private static readonly ParkingSpaceTracker _trackerInstance = new();       // The one and only instance, created when the class is first used
    private ParkingSpaceTracker() { }                                           // Private constructor: nobody outside can do "new ParkingSpaceTracker()"
    public static ParkingSpaceTracker Instance => _trackerInstance;             // The single access point

    private readonly Dictionary<VehicleType, int> _totalSlots = new();
    private readonly Dictionary<VehicleType, int> _availableSlots = new();

    public void Initialize(VehicleType type, int total, int available)
    {
        _totalSlots[type] = total;
        _availableSlots[type] = available;
    }

    public int GetTotal(VehicleType type) =>
        _totalSlots.TryGetValue(type, out var value) ? value : 0;

    public int GetAvailable(VehicleType type) =>
        _availableSlots.TryGetValue(type, out var value) ? value : 0;

    public bool TryOccupySlot(VehicleType type)
    {
        var available = GetAvailable(type);
        if (available == 0) 
        {
            return false;
        }

        _availableSlots[type] = available - 1;
        return true;
    }

    public bool TryReleaseSlot(VehicleType type)
    {
        var available = GetAvailable(type);
        var total = GetTotal(type);
        if (available >= total) 
        {
            return false;
        }

        _availableSlots[type] = available + 1;
        return true;
    }
    
}