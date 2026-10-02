using System.ComponentModel.DataAnnotations;

namespace ParkingSystem.Dtos;

public class PaymentRequest
{
    [Required]
    public string PaymentMethod { get; set; } = string.Empty;
}