using System;
using System.ComponentModel.DataAnnotations;

namespace ParkingSystem.Dashboard.Dto;

public class GetParkingActivityInput
{
    [Required]
    public DateTime FromDate { get; set; }

    [Required]
    public DateTime ToDate { get; set; }

    public TimeGroupingMode GroupBy { get; set; } = TimeGroupingMode.Day;
}
