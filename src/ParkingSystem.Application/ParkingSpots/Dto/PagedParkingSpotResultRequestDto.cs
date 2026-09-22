

using Abp.Application.Services.Dto;
using ParkingSystem.Entities.Enums;
using ParkingSystem.Validation;
using System.ComponentModel.DataAnnotations;

namespace ParkingSystem.ParkingSpots.Dto;

public class PagedParkingSpotResultRequestDto: PagedResultRequestDto, ISortedResultRequest
{
    public string Keyword { get; set; }
    public string Sorting { get; set; }

    [EnumDataType(typeof(ParkingSpotStatus))]
    public ParkingSpotStatus? Status { get; set; }

    [GreaterThanZero]
    public long? ParkingAreaId { get; set; }
}
