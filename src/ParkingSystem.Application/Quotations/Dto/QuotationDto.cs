

using Abp.Application.Services.Dto;
using ParkingSystem.Entities.Enums;
using System;
namespace ParkingSystem.Quotations.Dto;

public class QuotationDto: EntityDto<long>
{
    public VehicleType VehicleType { get; set; }
    public string VehicleTypeName => VehicleType.ToString();
    public int Duration { get; set; }
    public DurationUnit DurationUnit { get; set; }
    public string DurationUnitType => DurationUnit.ToString();
    public decimal Price { get; set; }
    public DateTime CreationTime { get; set; }
    public bool IsDeleted { get; set; }
}
