using System.ComponentModel.DataAnnotations;

namespace ParkingSystem.PaymentTransactions.Dto;

public class ProcessPaymentResultInput
{
    [Required]
    [StringLength(255)]
    public string TransactionCode { get; set; }

    public bool IsSuccess { get; set; }
}
