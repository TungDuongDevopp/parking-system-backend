using System;

namespace ParkingSystem.Dashboard.Dto;

public class ParkingActivityDto
{
    public string PeriodLabel { get; set; }
    public DateTime PeriodDate { get; set; }
    public int CheckInCount { get; set; }
    public int CheckOutCount { get; set; }
}
