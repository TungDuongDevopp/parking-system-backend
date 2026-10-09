

using Abp.Application.Services.Dto;
using ParkingSystem.Entities.Enums;
using System;

namespace ParkingSystem.ParkingSessions.Dto;

public class ParkingSessionDto : EntityDto<long>
{
    public string TicketCode { get; set; }
    public decimal Fee { get; set; }
    public long ParkingAreaId { get; set; }
    public string ParkingAreaCode { get; set; }
    public long? ParkingSpotId { get; set; }
    public string? ParkingSpotCode { get; set; }
    public string? PlateNumber { get; set; }
    public DateTime EntryTime { get; set; }
    public DateTime? ExitTime { get; set; }
    public ParkingSessionStatus Status { get; set; }
    public string ParkingSessionStatusName => Status.ToString();
    public long? CheckInStaffId { get; set; }
    public long? CheckOutStaffId { get; set; }
    public decimal? HourlyRate { get; set; }
    public decimal? DailyRate { get; set; }
}
