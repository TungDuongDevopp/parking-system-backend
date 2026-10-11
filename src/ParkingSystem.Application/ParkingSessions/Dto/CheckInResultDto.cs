using Abp.Application.Services.Dto;
using ParkingSystem.Entities.Enums;
using System;


namespace ParkingSystem.ParkingSessions.Dto;

public class CheckInResultDto : EntityDto<long>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public string? TicketCode { get; set; }
    public long? ParkingAreaId { get; set; }
    public long? ParkingSpotId { get; set; }
    public string? PlateNumber { get; set; }
    public DateTime? EntryTime { get; set; }
    public ParkingSessionStatus? Status { get; set; }
    public string ParkingSessionStatusName => Status != null ? Status.ToString(): null;
    public decimal? HourlyRate { get; set; }
    public decimal? DailyRate { get; set; }
    public long? CustomerId { get; set; }
   
}
