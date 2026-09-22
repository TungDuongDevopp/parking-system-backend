

using Abp.Application.Services.Dto;
using ParkingSystem.Entities.Enums;
using ParkingSystem.Validation;
using System.ComponentModel.DataAnnotations;

namespace ParkingSystem.Vehicles.Dto;

public class PagedVehicleResultRequestDto: PagedResultRequestDto, ISortedResultRequest
{
    public string Keyword { get; set; }
    public string Sorting { get; set; }

    [EnumDataType(typeof(VehicleType))]
    public VehicleType? VehicleType { get; set; }

    [GreaterThanZero]
    public long? CustomerId { get; set; }
}
