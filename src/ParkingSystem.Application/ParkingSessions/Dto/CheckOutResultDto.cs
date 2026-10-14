
using Abp.Application.Services.Dto;
using ParkingSystem.Entities.Enums;
using System;

namespace ParkingSystem.ParkingSessions.Dto;
public class CheckOutResultDto : EntityDto<long>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public string TicketCode { get; set; }
    public long ParkingAreaId { get; set; }
    public long? ParkingSpotId { get; set; }
    public long? CustomerId { get; set; }
    public long CheckOutStaffId { get; set; }
    public DateTime ExitTime { get; set; }
    public ParkingSessionStatus Status { get; set; }
    public string ParkingSessionStatusName => Status.ToString();
    public decimal? Fee { get; set; }
}
