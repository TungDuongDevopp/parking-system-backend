using Abp.MultiTenancy;
using Abp.Zero.Configuration;

namespace ParkingSystem.Authorization.Roles;

public static class AppRoleConfig
{
    public static void Configure(IRoleManagementConfig roleManagementConfig)
    {
        // Static host roles

        roleManagementConfig.StaticRoles.Add(
            new StaticRoleDefinition(
                StaticRoleNames.Host.Admin,
                MultiTenancySides.Host
            )
        );

        // Static tenant roles

        roleManagementConfig.StaticRoles.Add(
            new StaticRoleDefinition(
                StaticRoleNames.Tenants.Admin,
                MultiTenancySides.Tenant
            )
        );

        roleManagementConfig.StaticRoles.Add(
            new StaticRoleDefinition(
                StaticRoleNames.Tenants.Staff,
                MultiTenancySides.Tenant
            )
        );

        roleManagementConfig.StaticRoles.Add(
            new StaticRoleDefinition(
                StaticRoleNames.Tenants.Customer,
                MultiTenancySides.Tenant
            )
        );
    }
}
