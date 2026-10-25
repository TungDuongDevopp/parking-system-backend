using System.ComponentModel.DataAnnotations;

namespace ParkingSystem.Dashboard.Dto;

public class GetRecentActivitiesInput
{
    [Range(1, 50)]
    public int MaxCount { get; set; } = 10;
}
