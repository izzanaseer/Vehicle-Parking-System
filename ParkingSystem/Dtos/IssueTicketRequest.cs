using System.ComponentModel.DataAnnotations;
using ParkingSystem.Models;

namespace ParkingSystem.Dtos;

public class IssueTicketRequest
{
    [Required]
    public string VehicleNumber { get; set; } = string.Empty;

    [Required]
    public VehicleType VehicleType { get; set; }
}