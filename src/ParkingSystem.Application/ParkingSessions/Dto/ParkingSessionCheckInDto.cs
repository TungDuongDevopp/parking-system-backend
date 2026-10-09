
namespace ParkingSystem.ParkingSessions.Dto;
public class ParkingSessionCheckInDto
{
    public long ParkingAreaId { get; set; }
    public long? ParkingSpotId { get; set; }
    public string? PlateNumber { get; set; }
    public long? ReservationId { get; set; }
}
