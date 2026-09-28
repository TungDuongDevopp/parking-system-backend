using System.Threading.Tasks;
using Abp.Application.Navigation;
using Abp.Authorization;
using Abp.Localization;
using Abp.Threading;
using ParkingSystem.Authorization;

namespace ParkingSystem.Web.Startup;

/// <summary>
/// This class defines menus for the application.
/// </summary>
public class ParkingSystemNavigationProvider : NavigationProvider
{
    public override void SetNavigation(INavigationProviderContext context)
    {
        context.Manager.MainMenu
            .AddItem(
                new MenuItemDefinition(
                    PageNames.Home,
                    L("HomePage"),
                    url: "",
                    icon: "fas fa-home",
                    requiresAuthentication: true
                )
            ).AddItem(
                new MenuItemDefinition(
                    PageNames.Tenants,
                    L("Tenants"),
                    url: "Tenants",
                    icon: "fas fa-building",
                    permissionDependency: new SimplePermissionDependency(PermissionNames.Pages_Tenants)
                )
            ).AddItem(
                new MenuItemDefinition(
                    PageNames.Users,
                    L("Users"),
                    url: "Users",
                    icon: "fas fa-users",
                    permissionDependency: new SimplePermissionDependency(PermissionNames.Pages_Users)
                )
            ).AddItem(
                new MenuItemDefinition(
                    PageNames.Roles,
                    L("Roles"),
                    url: "Roles",
                    icon: "fas fa-theater-masks",
                    permissionDependency: new SimplePermissionDependency(PermissionNames.Pages_Roles)
                )
            ).AddItem(
                new MenuItemDefinition(
                    PageNames.Customers,
                    L("Customers"),
                    url: "Customer",
                    icon: "fas fa-address-book",
                    permissionDependency: new SimplePermissionDependency(PermissionNames.Pages_Customers_ViewAll)
                )
            ).AddItem(
                new MenuItemDefinition(
                    PageNames.CustomerProfile,
                    L("MyProfile"),
                    url: "Customer/Profile",
                    icon: "fas fa-id-card",
                    permissionDependency: new CustomerProfilePermissionDependency()
                )
            ).AddItem(
                new MenuItemDefinition(
                    PageNames.Staffs,
                    L("Staffs"),
                    url: "Staff",
                    icon: "fas fa-user-tie",
                    permissionDependency: new SimplePermissionDependency(PermissionNames.Pages_Staffs)
                )
            ).AddItem(
                new MenuItemDefinition(
                    PageNames.Vehicles,
                    L("Vehicles"),
                    url: "Vehicle",
                    icon: "fas fa-car",
                    permissionDependency: new SimplePermissionDependency(PermissionNames.Pages_Vehicles)
                )
            ).AddItem(
                new MenuItemDefinition(
                    PageNames.ParkingAreas,
                    L("ParkingAreas"),
                    url: "ParkingArea",
                    icon: "fas fa-parking",
                    permissionDependency: new SimplePermissionDependency(PermissionNames.Pages_ParkingAreas)
                    )
                ).AddItem(
                new MenuItemDefinition(
                    PageNames.ParkingSpots,
                    L("ParkingSpots"),
                    url: "ParkingSpot",
                    icon: "fas fa-square",
                    permissionDependency: new SimplePermissionDependency(PermissionNames.Pages_ParkingSpots)
                    )
                
                ).AddItem(
                new MenuItemDefinition(
                    PageNames.Quotations,
                    L("Quotations"),
                    url: "Quotation",
                    icon: "fas fa-tags",
                    permissionDependency: new SimplePermissionDependency(PermissionNames.Pages_Quotations)
                    )
                )
                .AddItem(
                new MenuItemDefinition(
                    PageNames.Subscriptions,
                    L("Subscription"),
                    url: "Subscription",
                    icon: "fas fa-id-card",
                    permissionDependency: new SimplePermissionDependency(PermissionNames.Pages_Subscriptions_Manager)
                    )
                )
                .AddItem(
                new MenuItemDefinition(
                    PageNames.MySubscription,
                    L("MySubscription"),
                    url: "Subscription/MySubscription",
                    icon: "fas fa-ticket-alt",
                    permissionDependency: new CustomerSubscriptionPermissionDependency()
                    )
                )
            ;

    }

    private static ILocalizableString L(string name)
    {
        return new LocalizableString(name, ParkingSystemConsts.LocalizationSourceName);
    }
}

public class CustomerProfilePermissionDependency : IPermissionDependency
{
    public bool IsSatisfied(IPermissionDependencyContext context)
    {
        return AsyncHelper.RunSync(() => IsSatisfiedAsync(context));
    }

    public async Task<bool> IsSatisfiedAsync(IPermissionDependencyContext context)
    {
        if (context.User == null)
        {
            return false;
        }

        // If the user has permission to view all customers (Staff, Manager, Admin),
        // they should see "Customers" management, not "My Profile".
        var canViewAll = await context.PermissionChecker.IsGrantedAsync(
            context.User,
            PermissionNames.Pages_Customers_ViewAll
        );

        if (canViewAll)
        {
            return false;
        }

        // Customer user must have base Customer permission
        return await context.PermissionChecker.IsGrantedAsync(
            context.User,
            PermissionNames.Pages_Customers
        );
    }
}

/// <summary>
/// Shows "My Subscription" only to customer users — i.e. those who have
/// Pages_Subscriptions but NOT Pages_Subscriptions_Manager.
/// </summary>
public class CustomerSubscriptionPermissionDependency : IPermissionDependency
{
    public bool IsSatisfied(IPermissionDependencyContext context)
    {
        return AsyncHelper.RunSync(() => IsSatisfiedAsync(context));
    }

    public async Task<bool> IsSatisfiedAsync(IPermissionDependencyContext context)
    {
        if (context.User == null)
            return false;

        // Managers/admins see the admin Subscription index, not this menu item.
        var isManager = await context.PermissionChecker.IsGrantedAsync(
            context.User,
            PermissionNames.Pages_Subscriptions_Manager
        );

        if (isManager)
            return false;

        return await context.PermissionChecker.IsGrantedAsync(
            context.User,
            PermissionNames.Pages_Subscriptions
        );
    }
}