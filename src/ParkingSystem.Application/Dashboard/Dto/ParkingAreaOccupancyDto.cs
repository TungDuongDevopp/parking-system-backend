using ParkingSystem.Entities.Enums;

namespace ParkingSystem.Dashboard.Dto;

public class ParkingAreaOccupancyDto
{
    public long Id { get; set; }
    public string ParkingCode { get; set; }
    public string Name { get; set; }
    public VehicleType VehicleType { get; set; }
    public ParkingMode ParkingMode { get; set; }
    public ParkingAreaStatus Status { get; set; }

    /// <summary>
    /// Sức chứa thiết kế của bãi.
    /// </summary>
    public int Capacity { get; set; }

    /// <summary>
    /// Số lượng xe thực sự đang đỗ trong bãi này.
    /// </summary>
    public int ActiveSessionsCount { get; set; }

    /// <summary>
    /// Số lượng chỗ đang được giữ trước bởi Reservation còn hiệu lực.
    /// </summary>
    public int ReservedCount { get; set; }

    /// <summary>
    /// Sức chứa bị chiếm dụng hoặc giữ chỗ (không thể sử dụng tiếp).
    /// </summary>
    public int UnavailableCapacity { get; set; }

    /// <summary>
    /// Sức chứa còn có thể tiếp nhận xe mới.
    /// </summary>
    public int AvailableCapacity { get; set; }

    /// <summary>
    /// Tỷ lệ sức chứa bị chiếm dụng (UnavailableCapacity / Capacity * 100).
    /// </summary>
    public double OccupancyPercentage { get; set; }
}
