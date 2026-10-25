namespace ParkingSystem.Dashboard.Dto;

public class DashboardSummaryDto
{
    /// <summary>
    /// Tổng sức chứa toàn bộ các bãi đang hoạt động (Active).
    /// </summary>
    public int TotalCapacity { get; set; }

    /// <summary>
    /// Số lượng xe thực sự đang đỗ trong bãi (ParkingSession Active, chưa checkout).
    /// </summary>
    public int ActiveSessionsCount { get; set; }

    /// <summary>
    /// Số lượng chỗ đang được giữ bởi Reservation còn hiệu lực.
    /// </summary>
    public int ReservedCount { get; set; }

    /// <summary>
    /// Tổng sức chứa đã bị chiếm dụng hoặc giữ chỗ (không thể sử dụng tiếp).
    /// </summary>
    public int UnavailableCapacity { get; set; }

    /// <summary>
    /// Sức chứa còn có thể sử dụng (TotalCapacity - UnavailableCapacity).
    /// </summary>
    public int AvailableCapacity { get; set; }

    /// <summary>
    /// Tỷ lệ sức chứa bị chiếm dụng (UnavailableCapacity / TotalCapacity * 100).
    /// </summary>
    public double OccupancyRate { get; set; }

    /// <summary>
    /// Tổng số ParkingArea đang hoạt động (Status = Active).
    /// </summary>
    public int ActiveParkingAreasCount { get; set; }

    /// <summary>
    /// Số Subscription đang có hiệu lực (Status = inUse, StartTime <= Now <= EndTime).
    /// </summary>
    public int ActiveSubscriptionsCount { get; set; }
}
