

using Abp.Application.Services.Dto;
using ParkingSystem.Entities.Enums;
using System;
namespace ParkingSystem.ParkingSpots.Dto;
public class ParkingSpotDto: EntityDto<long>
{
    public string SpotCode { get; set; }
    public ParkingSpotStatus Status { get; set; }
    public long ParkingAreaId { get; set; }
    public string ParkingAreaCode { get; set; }
    public string ParkingAreaName { get; set; }
    public DateTime CreationTime { get; set; }
    public bool IsDeleted { get; set; }

}
