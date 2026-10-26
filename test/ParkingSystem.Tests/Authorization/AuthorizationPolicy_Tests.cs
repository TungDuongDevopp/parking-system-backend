using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Authorization.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using ParkingSystem.Authorization.Roles;
using ParkingSystem.Authorization.Users;
using ParkingSystem.Dashboard;
using ParkingSystem.Dashboard.Dto;
using ParkingSystem.ParkingAreas;
using ParkingSystem.ParkingSpots;
using ParkingSystem.Subscriptions;
using Shouldly;
using Xunit;

namespace ParkingSystem.Tests.Authorization;

public class AuthorizationPolicy_Tests : ParkingSystemTestBase
{
    private readonly IDashboardAppService _dashboardAppService;
    private readonly ISubscriptionAppService _subscriptionAppService;
    private readonly IParkingAreaAppService _parkingAreaAppService;
    private readonly IParkingSpotAppService _parkingSpotAppService;

    public AuthorizationPolicy_Tests()
    {
        _dashboardAppService = Resolve<IDashboardAppService>();
        _subscriptionAppService = Resolve<ISubscriptionAppService>();
        _parkingAreaAppService = Resolve<IParkingAreaAppService>();
        _parkingSpotAppService = Resolve<IParkingSpotAppService>();

        SetupTestUsers();
    }

    private void SetupTestUsers()
    {
        UsingDbContext(context =>
        {
            var staffRole = context.Roles.FirstOrDefault(r => r.TenantId == 1 && r.Name == StaticRoleNames.Tenants.Staff);
            var customerRole = context.Roles.FirstOrDefault(r => r.TenantId == 1 && r.Name == StaticRoleNames.Tenants.Customer);

            var staffUser = context.Users.FirstOrDefault(u => u.TenantId == 1 && u.UserName == "test_staff");
            if (staffUser == null)
            {
                staffUser = new User
                {
                    TenantId = 1,
                    UserName = "test_staff",
                    NormalizedUserName = "TEST_STAFF",
                    Name = "Staff",
                    Surname = "Test",
                    EmailAddress = "staff@test.com",
                    NormalizedEmailAddress = "STAFF@TEST.COM",
                    IsActive = true,
                    IsEmailConfirmed = true
                };
                staffUser.Password = new PasswordHasher<User>(new OptionsWrapper<PasswordHasherOptions>(new PasswordHasherOptions())).HashPassword(staffUser, "Password123!");
                context.Users.Add(staffUser);
                context.SaveChanges();
                context.UserRoles.Add(new UserRole(1, staffUser.Id, staffRole.Id));
                context.SaveChanges();
            }

            var customerUser = context.Users.FirstOrDefault(u => u.TenantId == 1 && u.UserName == "test_customer");
            if (customerUser == null)
            {
                customerUser = new User
                {
                    TenantId = 1,
                    UserName = "test_customer",
                    NormalizedUserName = "TEST_CUSTOMER",
                    Name = "Customer",
                    Surname = "Test",
                    EmailAddress = "customer@test.com",
                    NormalizedEmailAddress = "CUSTOMER@TEST.COM",
                    IsActive = true,
                    IsEmailConfirmed = true
                };
                customerUser.Password = new PasswordHasher<User>(new OptionsWrapper<PasswordHasherOptions>(new PasswordHasherOptions())).HashPassword(customerUser, "Password123!");
                context.Users.Add(customerUser);
                context.SaveChanges();
                context.UserRoles.Add(new UserRole(1, customerUser.Id, customerRole.Id));
                context.SaveChanges();
            }
        });
    }

    private void LoginAsStaff()
    {
        LoginAsTenant("Default", "test_staff");
    }

    private void LoginAsCustomer()
    {
        LoginAsTenant("Default", "test_customer");
    }

    private void LogoutToAnonymous()
    {
        AbpSession.UserId = null;
        AbpSession.TenantId = 1;
    }

    #region 1. Anonymous Access Tests

    [Fact]
    public async Task Anonymous_Should_Be_Able_To_Access_AvailableQuotations()
    {
        // Arrange
        LogoutToAnonymous();

        // Act
        var quotations = await _subscriptionAppService.GetAvailableQuotationsAsync();

        // Assert
        quotations.ShouldNotBeNull();
    }

    [Fact]
    public async Task Anonymous_Should_Not_Be_Able_To_Access_SubscriptionLookup()
    {
        // Arrange
        LogoutToAnonymous();

        // Act & Assert
        await Should.ThrowAsync<AbpAuthorizationException>(async () =>
        {
            await _subscriptionAppService.GetSubscriptionLookUpAsync();
        });
    }

    [Fact]
    public async Task Anonymous_Should_Not_Be_Able_To_Access_DashboardSummary()
    {
        // Arrange
        LogoutToAnonymous();

        // Act & Assert
        await Should.ThrowAsync<AbpAuthorizationException>(async () =>
        {
            await _dashboardAppService.GetDashboardSummaryAsync();
        });
    }

    [Fact]
    public async Task Anonymous_Should_Not_Be_Able_To_Access_ParkingAreaLookup()
    {
        // Arrange
        LogoutToAnonymous();

        // Act & Assert
        await Should.ThrowAsync<AbpAuthorizationException>(async () =>
        {
            await _parkingAreaAppService.GetAvailableAreasAsync();
        });
    }

    #endregion

    #region 2. Customer Access Tests

    [Fact]
    public async Task Customer_Should_Not_Be_Able_To_Access_Dashboard_Endpoints()
    {
        // Arrange
        LoginAsCustomer();

        // Act & Assert - Dashboard Summary
        await Should.ThrowAsync<AbpAuthorizationException>(async () =>
        {
            await _dashboardAppService.GetDashboardSummaryAsync();
        });

        // Act & Assert - Parking Activity
        await Should.ThrowAsync<AbpAuthorizationException>(async () =>
        {
            await _dashboardAppService.GetParkingActivityAsync(new GetParkingActivityInput
            {
                FromDate = DateTime.Today.AddDays(-7),
                ToDate = DateTime.Today
            });
        });

        // Act & Assert - Parking Area Occupancy
        await Should.ThrowAsync<AbpAuthorizationException>(async () =>
        {
            await _dashboardAppService.GetParkingAreaOccupancyAsync();
        });

        // Act & Assert - Recent Activities
        await Should.ThrowAsync<AbpAuthorizationException>(async () =>
        {
            await _dashboardAppService.GetRecentActivitiesAsync(new GetRecentActivitiesInput());
        });
    }

    [Fact]
    public async Task Customer_Should_Not_Be_Able_To_Access_SubscriptionLookup()
    {
        // Arrange
        LoginAsCustomer();

        // Act & Assert
        await Should.ThrowAsync<AbpAuthorizationException>(async () =>
        {
            await _subscriptionAppService.GetSubscriptionLookUpAsync();
        });
    }

    [Fact]
    public async Task Customer_Should_Be_Able_To_Access_AvailableAreas_And_Spots()
    {
        // Arrange
        LoginAsCustomer();

        // Act
        var areas = await _parkingAreaAppService.GetAvailableAreasAsync();
        var spots = await _parkingSpotAppService.GetAvailableSpotsAsync(1);

        // Assert
        areas.ShouldNotBeNull();
        spots.ShouldNotBeNull();
    }

    #endregion

    #region 3. Staff Access Tests

    [Fact]
    public async Task Staff_Should_Not_Be_Able_To_Access_Admin_DashboardSummary_Or_ActivityChart()
    {
        // Arrange
        LoginAsStaff();

        // Act & Assert - Dashboard Summary (contains total business subscriptions count)
        await Should.ThrowAsync<AbpAuthorizationException>(async () =>
        {
            await _dashboardAppService.GetDashboardSummaryAsync();
        });

        // Act & Assert - System-wide parking activity chart
        await Should.ThrowAsync<AbpAuthorizationException>(async () =>
        {
            await _dashboardAppService.GetParkingActivityAsync(new GetParkingActivityInput
            {
                FromDate = DateTime.Today.AddDays(-7),
                ToDate = DateTime.Today
            });
        });
    }

    [Fact]
    public async Task Staff_Should_Be_Able_To_Access_Operational_Dashboard_Endpoints()
    {
        // Arrange
        LoginAsStaff();

        // Act
        var occupancy = await _dashboardAppService.GetParkingAreaOccupancyAsync();
        var recentActivities = await _dashboardAppService.GetRecentActivitiesAsync(new GetRecentActivitiesInput());
        var areas = await _parkingAreaAppService.GetAvailableAreasAsync();

        // Assert
        occupancy.ShouldNotBeNull();
        recentActivities.ShouldNotBeNull();
        areas.ShouldNotBeNull();
    }

    #endregion

    #region 4. Admin Access Tests

    [Fact]
    public async Task Admin_Should_Have_Full_Access_To_All_Endpoints()
    {
        // Arrange
        LoginAsDefaultTenantAdmin();

        // Act
        var summary = await _dashboardAppService.GetDashboardSummaryAsync();
        var activity = await _dashboardAppService.GetParkingActivityAsync(new GetParkingActivityInput
        {
            FromDate = DateTime.Today.AddDays(-7),
            ToDate = DateTime.Today
        });
        var occupancy = await _dashboardAppService.GetParkingAreaOccupancyAsync();
        var recent = await _dashboardAppService.GetRecentActivitiesAsync(new GetRecentActivitiesInput());
        var subLookup = await _subscriptionAppService.GetSubscriptionLookUpAsync();
        var areas = await _parkingAreaAppService.GetAvailableAreasAsync();

        // Assert
        summary.ShouldNotBeNull();
        activity.ShouldNotBeNull();
        occupancy.ShouldNotBeNull();
        recent.ShouldNotBeNull();
        subLookup.ShouldNotBeNull();
        areas.ShouldNotBeNull();
    }

    #endregion
}
