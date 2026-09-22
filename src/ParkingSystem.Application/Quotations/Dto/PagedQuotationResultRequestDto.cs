

using Abp.Application.Services.Dto;
using ParkingSystem.Entities.Enums;
using ParkingSystem.Validation;
using System.ComponentModel.DataAnnotations;


namespace ParkingSystem.Quotations.Dto;

public class PagedQuotationResultRequestDto: PagedResultRequestDto, ISortedResultRequest
{
    public string Keyword { get; set; }
    public string Sorting { get; set; }

    [EnumDataType(typeof(VehicleType))]
    public VehicleType? VehicleType { get; set; }

    [GreaterThanZero]
    public int? Duration { get; set; }

    [EnumDataType(typeof(DurationUnit))]
    public DurationUnit? DurationUnit { get; set; }

    [GreaterThanZero]
    public decimal? MinPrice { get; set; }

    [GreaterThanZero]
    public decimal? MaxPrice { get; set; }
}
