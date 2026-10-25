using System;

namespace ParkingSystem.Dashboard.Dto;

public class RecentActivityDto
{
    public ActivityType ActivityType { get; set; }
    public long ParkingSessionId { get; set; }
    public string TicketCode { get; set; }
    public string PlateNumber { get; set; }
    public long ParkingAreaId { get; set; }
    public string ParkingAreaName { get; set; }
    public string SpotCode { get; set; }
    public long? StaffId { get; set; }
    public string StaffName { get; set; }
    public DateTime EventTime { get; set; }
    public DateTime LocalEventTime { get; set; }
}
