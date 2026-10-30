using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Timing;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using ParkingSystem.Authorization;
using ParkingSystem.Dashboard.Dto;
using ParkingSystem.Entities;
using ParkingSystem.Entities.Enums;
using ParkingSystem.Exceptions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace ParkingSystem.Dashboard;

[AbpAuthorize(PermissionNames.Pages_Dashboard_Admin, PermissionNames.Pages_Dashboard_Staff)]
public class DashboardAppService : ParkingSystemAppServiceBase, IDashboardAppService
{
    private readonly IRepository<ParkingArea, long> _parkingAreaRepository;
    private readonly IRepository<ParkingSpot, long> _parkingSpotRepository;
    private readonly IRepository<ParkingSession, long> _parkingSessionRepository;
    private readonly IRepository<Reservation, long> _reservationRepository;
    private readonly IRepository<Subscription, long> _subscriptionRepository;

    private static readonly TimeZoneInfo LocalTimeZone = GetLocalTimeZone();

    public DashboardAppService(
        IRepository<ParkingArea, long> parkingAreaRepository,
        IRepository<ParkingSpot, long> parkingSpotRepository,
        IRepository<ParkingSession, long> parkingSessionRepository,
        IRepository<Reservation, long> reservationRepository,
        IRepository<Subscription, long> subscriptionRepository)
    {
        _parkingAreaRepository = parkingAreaRepository;
        _parkingSpotRepository = parkingSpotRepository;
        _parkingSessionRepository = parkingSessionRepository;
        _reservationRepository = reservationRepository;
        _subscriptionRepository = subscriptionRepository;
    }

    private static TimeZoneInfo GetLocalTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }
    }

    [AbpAuthorize(PermissionNames.Pages_Dashboard_Admin)]
    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync()
    {
        var nowUtc = Clock.Now;

        // 1. Lấy danh sách tất cả các bãi đang Active và chưa bị xóa
        var activeAreas = await _parkingAreaRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.Status == ParkingAreaStatus.Active)
            .ToListAsync();

        var totalCapacity = activeAreas.Sum(x => x.Capacity);
        var activeParkingAreasCount = activeAreas.Count;

        // 2. Số lượng xe thực sự đang đỗ trong toàn hệ thống (Active, chưa checkout)
        var activeSessionsCount = await _parkingSessionRepository.GetAll()
            .AsNoTracking()
            .CountAsync(x => x.Status == ParkingSessionStatus.Active && x.ExitTime == null);

        // 3. Số lượng chỗ đang được giữ bởi Reservation còn hiệu lực
        var reservedCount = await _reservationRepository.GetAll()
            .AsNoTracking()
            .CountAsync(x => x.Status == ReservationStatus.Reserved && x.ExpireAt > nowUtc);

        // 4. Tính toán UnavailableCapacity (sức chứa bị chiếm dụng hoặc giữ chỗ)
        // Với bãi capacityBase: CurrentOccupancy lưu trữ số chỗ đã dùng (gồm cả xe vào bãi và reservation)
        var capacityBaseUnavailable = activeAreas
            .Where(a => a.ParkingMode == ParkingMode.capacityBase)
            .Sum(a => Math.Clamp(a.CurrentOccupancy, 0, a.Capacity));

        // Với bãi individualSpot: Đếm số spot đang ở trạng thái Occupied, Reserved, Unavailable
        var individualAreaIds = activeAreas
            .Where(a => a.ParkingMode == ParkingMode.individualSpot)
            .Select(a => a.Id)
            .ToList();

        var individualUnavailable = 0;
        if (individualAreaIds.Count > 0)
        {
            individualUnavailable = await _parkingSpotRepository.GetAll()
                .AsNoTracking()
                .Where(s => individualAreaIds.Contains(s.ParkingAreaId) &&
                            (s.Status == ParkingSpotStatus.Occupied ||
                             s.Status == ParkingSpotStatus.Reserved ||
                             s.Status == ParkingSpotStatus.Unavailable))
                .CountAsync();
        }

        var unavailableCapacity = Math.Min(totalCapacity, capacityBaseUnavailable + individualUnavailable);
        var availableCapacity = Math.Max(0, totalCapacity - unavailableCapacity);
        var occupancyRate = totalCapacity > 0
            ? Math.Round((double)unavailableCapacity / totalCapacity * 100, 2)
            : 0;

        // 5. Số Subscription đang có hiệu lực
        var activeSubscriptionsCount = await _subscriptionRepository.GetAll()
            .AsNoTracking()
            .CountAsync(x => x.Status == SubscriptionStatus.inUse &&
                             x.StartTime <= nowUtc &&
                             x.EndTime >= nowUtc);

        return new DashboardSummaryDto
        {
            TotalCapacity = totalCapacity,
            ActiveSessionsCount = activeSessionsCount,
            ReservedCount = reservedCount,
            UnavailableCapacity = unavailableCapacity,
            AvailableCapacity = availableCapacity,
            OccupancyRate = occupancyRate,
            ActiveParkingAreasCount = activeParkingAreasCount,
            ActiveSubscriptionsCount = activeSubscriptionsCount
        };
    }

    [AbpAuthorize(PermissionNames.Pages_Dashboard_Admin, PermissionNames.Pages_Dashboard_Staff)]
    public async Task<List<ParkingAreaOccupancyDto>> GetParkingAreaOccupancyAsync()
    {
        var nowUtc = Clock.Now;

        // Lấy danh sách toàn bộ bãi xe chưa bị xóa
        var areas = await _parkingAreaRepository.GetAll()
            .AsNoTracking()
            .OrderBy(a => a.ParkingCode)
            .ToListAsync();

        if (areas.Count == 0)
        {
            return new List<ParkingAreaOccupancyDto>();
        }

        // Lấy số lượng xe đang đỗ thực tế theo từng bãi
        var activeSessionCounts = await _parkingSessionRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.Status == ParkingSessionStatus.Active && x.ExitTime == null)
            .GroupBy(x => x.ParkingAreaId)
            .Select(g => new { ParkingAreaId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ParkingAreaId, x => x.Count);

        // Lấy số lượng reservation đang hiệu lực theo từng bãi
        var activeReservationCounts = await _reservationRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.Status == ReservationStatus.Reserved && x.ExpireAt > nowUtc)
            .GroupBy(x => x.ParkingAreaId)
            .Select(g => new { ParkingAreaId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ParkingAreaId, x => x.Count);

        // Lấy thống kê trạng thái spot theo từng bãi
        var spotStatusByArea = await _parkingSpotRepository.GetAll()
            .AsNoTracking()
            .GroupBy(s => new { s.ParkingAreaId, s.Status })
            .Select(g => new { g.Key.ParkingAreaId, g.Key.Status, Count = g.Count() })
            .ToListAsync();

        var result = new List<ParkingAreaOccupancyDto>();

        foreach (var area in areas)
        {
            int activeSessions;
            int reservedCount;
            int unavailable;
            int available;

            if (area.ParkingMode == ParkingMode.capacityBase)
            {
                activeSessions = activeSessionCounts.GetValueOrDefault(area.Id, 0);
                reservedCount = activeReservationCounts.GetValueOrDefault(area.Id, 0);
                unavailable = Math.Clamp(area.CurrentOccupancy, 0, area.Capacity);
                available = Math.Max(0, area.Capacity - unavailable);
            }
            else
            {
                var spots = spotStatusByArea.Where(s => s.ParkingAreaId == area.Id).ToList();
                var occupiedSpots = spots.FirstOrDefault(s => s.Status == ParkingSpotStatus.Occupied)?.Count ?? 0;
                var reservedSpots = spots.FirstOrDefault(s => s.Status == ParkingSpotStatus.Reserved)?.Count ?? 0;
                var unavailableSpots = spots.FirstOrDefault(s => s.Status == ParkingSpotStatus.Unavailable)?.Count ?? 0;

                activeSessions = occupiedSpots;
                reservedCount = reservedSpots;
                unavailable = Math.Min(area.Capacity, occupiedSpots + reservedSpots + unavailableSpots);
                available = Math.Max(0, area.Capacity - unavailable);
            }

            var occupancyPercentage = area.Capacity > 0
                ? Math.Round((double)unavailable / area.Capacity * 100, 2)
                : 0;

            result.Add(new ParkingAreaOccupancyDto
            {
                Id = area.Id,
                ParkingCode = area.ParkingCode,
                Name = area.Name,
                VehicleType = area.VehicleType,
                ParkingMode = area.ParkingMode,
                Status = area.Status,
                Capacity = area.Capacity,
                ActiveSessionsCount = activeSessions,
                ReservedCount = reservedCount,
                UnavailableCapacity = unavailable,
                AvailableCapacity = available,
                OccupancyPercentage = occupancyPercentage
            });
        }

        return result;
    }

    [AbpAuthorize(PermissionNames.Pages_Dashboard_Admin)]
    public async Task<List<ParkingActivityDto>> GetParkingActivityAsync(GetParkingActivityInput input)
    {
        if (input.FromDate > input.ToDate)
        {
            throw new UserFriendlyException("FromDate must be less than or equal to ToDate");
        }
        var duration = input.ToDate - input.FromDate;
        if(input.GroupBy == TimeGroupingMode.Day && duration.TotalDays>90)
        {
            throw new BusinessRuleException("You can't search over 90 day in Day mode ");
        }
        if(input.GroupBy == TimeGroupingMode.Week && duration.TotalDays>365)
        {
            throw new BusinessRuleException("You can't search over 1 year in Week mode ");
        }
        if(input.GroupBy == TimeGroupingMode.Month && duration.TotalDays>1095)
        {
            throw new BusinessRuleException("You can't search over 3 year in Month mode ");
        }
        // Quy đổi ngày bắt đầu và kết thúc theo Múi giờ Việt Nam (UTC+7)
        var localStart = new DateTime(input.FromDate.Year, input.FromDate.Month, input.FromDate.Day, 0, 0, 0, DateTimeKind.Unspecified);
        var localEnd = new DateTime(input.ToDate.Year, input.ToDate.Month, input.ToDate.Day, 23, 59, 59, 999, DateTimeKind.Unspecified);

        var utcStart = TimeZoneInfo.ConvertTimeToUtc(localStart, LocalTimeZone);
        var utcEnd = TimeZoneInfo.ConvertTimeToUtc(localEnd, LocalTimeZone);

        // 1. Lấy danh sách EntryTime của các lượt Check-in trong khoảng thời gian
        var checkInUtcTimes = await _parkingSessionRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.EntryTime >= utcStart && x.EntryTime <= utcEnd)
            .Select(x => x.EntryTime)
            .ToListAsync();

        // 2. Lấy danh sách ExitTime của các lượt Check-out trong khoảng thời gian
        var checkOutUtcTimes = await _parkingSessionRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.Status == ParkingSessionStatus.Paid &&
                        x.ExitTime != null &&
                        x.ExitTime >= utcStart &&
                        x.ExitTime <= utcEnd)
            .Select(x => x.ExitTime.Value)
            .ToListAsync();

        // Chuyển đổi toàn bộ mốc thời gian sang giờ địa phương Việt Nam
        var checkInLocalTimes = checkInUtcTimes
            .Select(t => TimeZoneInfo.ConvertTimeFromUtc(t, LocalTimeZone))
            .ToList();

        var checkOutLocalTimes = checkOutUtcTimes
            .Select(t => TimeZoneInfo.ConvertTimeFromUtc(t, LocalTimeZone))
            .ToList();

        var result = new List<ParkingActivityDto>();

        switch (input.GroupBy)
        {
            case TimeGroupingMode.Day:
            {
                for (var date = localStart.Date; date <= localEnd.Date; date = date.AddDays(1))
                {
                    var nextDate = date.AddDays(1);
                    var checkIns = checkInLocalTimes.Count(t => t >= date && t < nextDate);
                    var checkOuts = checkOutLocalTimes.Count(t => t >= date && t < nextDate);

                    result.Add(new ParkingActivityDto
                    {
                        PeriodLabel = date.ToString("yyyy-MM-dd"),
                        PeriodDate = date,
                        CheckInCount = checkIns,
                        CheckOutCount = checkOuts
                    });
                }
                break;
            }

            case TimeGroupingMode.Week:
            {
                var current = localStart.Date;
                // Tìm ngày đầu tuần (Thứ 2)
                var diff = (7 + (current.DayOfWeek - DayOfWeek.Monday)) % 7;
                var weekStart = current.AddDays(-1 * diff);

                var calendar = CultureInfo.InvariantCulture.Calendar;

                while (weekStart <= localEnd.Date)
                {
                    var weekEnd = weekStart.AddDays(7);
                    var weekNum = calendar.GetWeekOfYear(weekStart, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

                    var checkIns = checkInLocalTimes.Count(t => t >= weekStart && t < weekEnd);
                    var checkOuts = checkOutLocalTimes.Count(t => t >= weekStart && t < weekEnd);

                    result.Add(new ParkingActivityDto
                    {
                        PeriodLabel = $"W{weekNum}-{weekStart:yyyy}",
                        PeriodDate = weekStart,
                        CheckInCount = checkIns,
                        CheckOutCount = checkOuts
                    });

                    weekStart = weekEnd;
                }
                break;
            }

            case TimeGroupingMode.Month:
            {
                var monthStart = new DateTime(localStart.Year, localStart.Month, 1);
                var lastMonth = new DateTime(localEnd.Year, localEnd.Month, 1);

                while (monthStart <= lastMonth)
                {
                    var nextMonth = monthStart.AddMonths(1);
                    var checkIns = checkInLocalTimes.Count(t => t >= monthStart && t < nextMonth);
                    var checkOuts = checkOutLocalTimes.Count(t => t >= monthStart && t < nextMonth);

                    result.Add(new ParkingActivityDto
                    {
                        PeriodLabel = monthStart.ToString("yyyy-MM"),
                        PeriodDate = monthStart,
                        CheckInCount = checkIns,
                        CheckOutCount = checkOuts
                    });

                    monthStart = nextMonth;
                }
                break;
            }
        }

        return result;
    }

    [AbpAuthorize(PermissionNames.Pages_Dashboard_Admin, PermissionNames.Pages_Dashboard_Staff)]
    public async Task<List<RecentActivityDto>> GetRecentActivitiesAsync(GetRecentActivitiesInput input)
    {
        var maxCount = Math.Clamp(input.MaxCount, 1, 50);

        // 1. Lấy Top N sự kiện Check-in mới nhất
        var checkIns = await _parkingSessionRepository.GetAll()
            .AsNoTracking()
            .OrderByDescending(x => x.EntryTime)
            .Take(maxCount)
            .Select(x => new RecentActivityDto
            {
                ActivityType = ActivityType.CheckIn,
                ParkingSessionId = x.Id,
                TicketCode = x.TicketCode,
                PlateNumber = x.PlateNumber,
                ParkingAreaId = x.ParkingAreaId,
                ParkingAreaName = x.ParkingArea.Name,
                SpotCode = x.ParkingSpot != null ? x.ParkingSpot.SpotCode : null,
                StaffId = x.CheckInStaffId,
                StaffName = x.CheckInStaff != null ? x.CheckInStaff.Name : null,
                EventTime = x.EntryTime
            })
            .ToListAsync();

        // 2. Lấy Top N sự kiện Check-out mới nhất
        var checkOuts = await _parkingSessionRepository.GetAll()
            .AsNoTracking()
            .Where(x => x.Status == ParkingSessionStatus.Paid && x.ExitTime != null)
            .OrderByDescending(x => x.ExitTime)
            .Take(maxCount)
            .Select(x => new RecentActivityDto
            {
                ActivityType = ActivityType.CheckOut,
                ParkingSessionId = x.Id,
                TicketCode = x.TicketCode,
                PlateNumber = x.PlateNumber,
                ParkingAreaId = x.ParkingAreaId,
                ParkingAreaName = x.ParkingArea.Name,
                SpotCode = x.ParkingSpot != null ? x.ParkingSpot.SpotCode : null,
                StaffId = x.CheckOutStaffId,
                StaffName = x.CheckOutStaff != null ? x.CheckOutStaff.Name : null,
                EventTime = x.ExitTime.Value
            })
            .ToListAsync();

        // 3. Hợp nhất 2 danh sách, quy đổi múi giờ địa phương và lấy Top N theo thời gian thực
        var merged = checkIns
            .Concat(checkOuts)
            .OrderByDescending(x => x.EventTime)
            .Take(maxCount)
            .ToList();

        foreach (var item in merged)
        {
            item.LocalEventTime = TimeZoneInfo.ConvertTimeFromUtc(item.EventTime, LocalTimeZone);
        }

        return merged;
    }
}
