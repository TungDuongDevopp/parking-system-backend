using System.ComponentModel.DataAnnotations;

namespace ParkingSystem.PaymentTransactions.Dto;

public class RetryPaymentInput
{
    [Required]
    public long PaymentId { get; set; }
}
