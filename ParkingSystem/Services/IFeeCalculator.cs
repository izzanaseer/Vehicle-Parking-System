using ParkingSystem.Models;

namespace ParkingSystem.Services;

public interface IFeeCalculator
{
    /*
    An interface defines what a method must do (its name, parameters, return type) without saying how. Any class that implements 
    this interface must provide a CalculateFee method matching this exact signature.
    */
    decimal CalculateFee(VehicleType type, DateTime entryTime, DateTime exitTime, decimal ratePerHour);
}