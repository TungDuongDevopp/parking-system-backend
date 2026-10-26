using Abp.Authorization;
using Abp.Authorization.Roles;
using Abp.Authorization.Users;
using Abp.MultiTenancy;
using ParkingSystem.Authorization;
using ParkingSystem.Authorization.Roles;
using ParkingSystem.Authorization.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using ParkingSystem.Entities;

namespace ParkingSystem.EntityFrameworkCore.Seed.Tenants;

public class TenantRoleAndUserBuilder
{
    private readonly ParkingSystemDbContext _context;
    private readonly int _tenantId;

    public TenantRoleAndUserBuilder(ParkingSystemDbContext context, int tenantId)
    {
        _context = context;
        _tenantId = tenantId;
    }

    public void Create()
    {
        CreateRolesAndUsers();
    }

    private void CreateRolesAndUsers()
    {
        // Admin role

        var adminRole = _context.Roles.IgnoreQueryFilters().FirstOrDefault(r => r.TenantId == _tenantId && r.Name == StaticRoleNames.Tenants.Admin);
        if (adminRole == null)
        {
            adminRole = _context.Roles.Add(new Role(_tenantId, StaticRoleNames.Tenants.Admin, StaticRoleNames.Tenants.Admin) { IsStatic = true }).Entity;
            _context.SaveChanges();
        }

        var staffRole = _context.Roles.IgnoreQueryFilters().FirstOrDefault(r => r.TenantId == _tenantId && r.Name == StaticRoleNames.Tenants.Staff);
        if (staffRole == null)
        {
            staffRole = _context.Roles.Add(new Role(_tenantId, StaticRoleNames.Tenants.Staff, StaticRoleNames.Tenants.Staff) { IsStatic = true }).Entity;
            _context.SaveChanges();
        }

        var customerRole = _context.Roles.IgnoreQueryFilters().FirstOrDefault(r => r.TenantId == _tenantId && r.Name == StaticRoleNames.Tenants.Customer);
        if (customerRole == null)
        {
            customerRole = _context.Roles.Add(new Role(_tenantId, StaticRoleNames.Tenants.Customer, StaticRoleNames.Tenants.Customer) { IsStatic = true, IsDefault = true }).Entity;
            _context.SaveChanges();
        }

        // Grant all permissions to admin role

        var grantedPermissions = _context.Permissions.IgnoreQueryFilters()
            .OfType<RolePermissionSetting>()
            .Where(p => p.TenantId == _tenantId && p.RoleId == adminRole.Id)
            .Select(p => p.Name)
            .ToList();

        var permissions = PermissionFinder
            .GetAllPermissions(new ParkingSystemAuthorizationProvider())
            .Where(p => p.MultiTenancySides.HasFlag(MultiTenancySides.Tenant) &&
                        !grantedPermissions.Contains(p.Name))
            .ToList();

        if (permissions.Any())
        {
            _context.Permissions.AddRange(
                permissions.Select(permission => new RolePermissionSetting
                {
                    TenantId = _tenantId,
                    Name = permission.Name,
                    IsGranted = true,
                    RoleId = adminRole.Id
                })
            );
            _context.SaveChanges();
        }

        // Grant operational permissions to staff role
        var staffPermissions = new List<string>
        {
            PermissionNames.Pages_Dashboard_Staff,
            PermissionNames.Pages_Staffs,
            PermissionNames.Pages_ParkingAreas,
            PermissionNames.Pages_ParkingAreas_Lookup,
            PermissionNames.Pages_ParkingSpots,
            PermissionNames.Pages_ParkingSpots_Lookup,
            PermissionNames.Pages_Quotations,
            PermissionNames.Pages_Subscriptions,
            PermissionNames.Pages_Subscription_Plate,
            PermissionNames.Pages_Reservations,
            PermissionNames.Pages_Reservations_ViewAll,
            PermissionNames.Pages_ParkingSessions,
            PermissionNames.Pages_ParkingSessions_ViewAll,
            PermissionNames.Pages_ParkingSessions_Image,
            PermissionNames.Pages_ParkingSession_Image_ViewAll
        };

        var staffGranted = _context.Permissions.IgnoreQueryFilters()
            .OfType<RolePermissionSetting>()
            .Where(p => p.TenantId == _tenantId && p.RoleId == staffRole.Id)
            .Select(p => p.Name)
            .ToList();

        var staffMissing = staffPermissions.Where(p => !staffGranted.Contains(p)).ToList();
        if (staffMissing.Any())
        {
            _context.Permissions.AddRange(
                staffMissing.Select(permissionName => new RolePermissionSetting
                {
                    TenantId = _tenantId,
                    Name = permissionName,
                    IsGranted = true,
                    RoleId = staffRole.Id
                })
            );
            _context.SaveChanges();
        }

        // Grant personal & lookup permissions to customer role
        var customerPermissions = new List<string>
        {
            PermissionNames.Pages_Customers,
            PermissionNames.Pages_Customers_ModifyOwn,
            PermissionNames.Pages_Reservations,
            PermissionNames.Pages_Reservations_ModifyOwn,
            PermissionNames.Pages_Subscriptions,
            PermissionNames.Pages_Subscription_Plate,
            PermissionNames.Pages_Subscription_Plate_Add,
            PermissionNames.Pages_Subscription_Plate_Change,
            PermissionNames.Pages_Subscription_Plate_ViewOwn,
            PermissionNames.Pages_ParkingSessions,
            PermissionNames.Pages_ParkingSessions_ViewOwn,
            PermissionNames.Pages_ParkingAreas_Lookup,
            PermissionNames.Pages_ParkingSpots_Lookup
        };

        var customerGranted = _context.Permissions.IgnoreQueryFilters()
            .OfType<RolePermissionSetting>()
            .Where(p => p.TenantId == _tenantId && p.RoleId == customerRole.Id)
            .Select(p => p.Name)
            .ToList();

        var customerMissing = customerPermissions.Where(p => !customerGranted.Contains(p)).ToList();
        if (customerMissing.Any())
        {
            _context.Permissions.AddRange(
                customerMissing.Select(permissionName => new RolePermissionSetting
                {
                    TenantId = _tenantId,
                    Name = permissionName,
                    IsGranted = true,
                    RoleId = customerRole.Id
                })
            );
            _context.SaveChanges();
        }

        // Admin user

        var adminUser = _context.Users.IgnoreQueryFilters().FirstOrDefault(u => u.TenantId == _tenantId && u.UserName == AbpUserBase.AdminUserName);
        if (adminUser == null)
        {
            adminUser = User.CreateTenantAdminUser(_tenantId, "admin@defaulttenant.com");
            adminUser.Password = new PasswordHasher<User>(new OptionsWrapper<PasswordHasherOptions>(new PasswordHasherOptions())).HashPassword(adminUser, "123qwe");
            adminUser.IsEmailConfirmed = true;
            adminUser.IsActive = true;

            _context.Users.Add(adminUser);
            _context.SaveChanges();

            // Assign Admin role to admin user
            _context.UserRoles.Add(new UserRole(_tenantId, adminUser.Id, adminRole.Id));
            _context.SaveChanges();
        }
    }
}
