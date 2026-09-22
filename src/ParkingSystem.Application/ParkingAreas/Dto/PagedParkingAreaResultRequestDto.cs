

using Abp.Application.Services.Dto;
using ParkingSystem.Entities.Enums;
using ParkingSystem.Validation;
using System.ComponentModel.DataAnnotations;

namespace ParkingSystem.ParkingAreas.Dto;

public class PagedParkingAreaResultRequestDto: PagedResultRequestDto, ISortedResultRequest
{
    public string Keyword { get; set; }
    public string Sorting { get; set; }

    [EnumDataType(typeof(VehicleType))]
    public VehicleType? VehicleType { get; set; }

    [GreaterThanZero]
    public int? MinCapacity { get; set; }

    [GreaterThanZero]
    public int? MaxCapacity { get; set; }

    [EnumDataType(typeof(ParkingMode))]
    public ParkingMode? ParkingMode { get; set; }
}
