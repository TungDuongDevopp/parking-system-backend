using Abp.Application.Services.Dto;
using ParkingSystem.Validation;
using System.ComponentModel.DataAnnotations;


namespace ParkingSystem.SubscriptionPlate.Dto;
public class ChangePlateDto : EntityDto<long>
{
    [GreaterThanZero]
    public long SubscriptionId { get; set; }

    [Required]
    [StringLength(12)]
    public string PlateNumber { get; set; }

}
