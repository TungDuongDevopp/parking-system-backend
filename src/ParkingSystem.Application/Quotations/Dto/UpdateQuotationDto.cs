
using Abp.Application.Services.Dto;
using ParkingSystem.Entities.Enums;
using ParkingSystem.Validation;
using System.ComponentModel.DataAnnotations;

namespace ParkingSystem.Quotations.Dto;

public class UpdateQuotationDto : EntityDto<long>
{
    [EnumDataType(typeof(VehicleType))]
    public VehicleType? VehicleType { get; set; }

    [GreaterThanZero]
    public int? Duration { get; set; }

    [EnumDataType(typeof(DurationUnit))]
    public DurationUnit? DurationUnit { get; set; }
    [GreaterThanZero]
    public decimal? Price { get; set; }
}