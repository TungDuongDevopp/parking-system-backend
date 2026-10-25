

using Abp.Application.Services.Dto;
using ParkingSystem.Entities.Enums;
using ParkingSystem.Validation;
using System;
using System.ComponentModel.DataAnnotations;

namespace ParkingSystem.ParkingSessions.Dto;

public class PagedParkingSessionResultRequestDto : PagedResultRequestDto, ISortedResultRequest
{
    public string Keyword { get; set; }
    public string Sorting { get; set; }

    [GreaterThanZero]
    public long? CustomerId { get; set; }

    [GreaterThanZero]
    public long? ParkingSpotId { get; set; }

    [GreaterThanZero]
    public long? ParkingAreaId { get; set; }

    [GreaterThanZero]
    public decimal? MinFee { get; set; }
    [GreaterThanZero]
    public decimal? MaxFee { get; set; }

    [GreaterThanZero]
    public decimal? MinHourlyRate { get; set; }
    [GreaterThanZero]
    public decimal? MaxHourlyRate { get; set; }
    
    [GreaterThanZero]
    public decimal? MinDailyRate { get; set; }
    [GreaterThanZero]
    public decimal? MaxDailyRate { get; set; }

    public DateTime? EntryFrom { get; set; }

    public DateTime? EntryTo { get; set; }

    public DateTime? ExitFrom { get; set; }

    public DateTime? ExitTo { get; set; }

    [EnumDataType(typeof(ParkingSessionStatus))]
    public ParkingSessionStatus? Status { get; set; }
    [GreaterThanZero]
    public long? CheckInStaffId { get; set; }
    [GreaterThanZero]
    public long? CheckOutStaffId { get; set; }
}
