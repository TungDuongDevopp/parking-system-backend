namespace ParkingSystem.Authorization;

/// <summary>
/// This class defines name constants for the permissions used in the application.
/// </summary>
public static class PermissionNames
{
    
    public const string Pages_Tenants = "Pages.Tenants";

    public const string Pages_Users = "Pages.Users";
    public const string Pages_Users_Activation = "Pages.Users.Activation";

    public const string Pages_Roles = "Pages.Roles";

    //Customer
    public const string Pages_Customers = "Pages.Customers";

    public const string Pages_Customers_ViewAll = "Pages.Customers.ViewAll";

    public const string Pages_Customers_ModifyAll = "Pages.Customers.ModifyAll";

    public const string Pages_Customers_ModifyOwn = "Pages.Customers.ModifyOwn";


    //Staff
    public const string Pages_Staffs = "Pages.Staffs";

    public const string Pages_Staffs_Manager = "Pages.Staffs.Manager";


    //Parking Area
    public const string Pages_ParkingAreas = "Pages.ParkingAreas";
    public const string Pages_ParkingAreas_Manager = "Pages.ParkingAreas.Manager";

    //Parking Spot
    public const string Pages_ParkingSpots = "Pages.ParkingSpots";
    public const string Pages_ParkingSpots_Manager = "Pages.ParkingSpots.Manager";

    //Quotation
    public const string Pages_Quotations = "Pages.Quotations";
    public const string Pages_Quotations_Manager = "Pages.Quotations.Manager";

    //SubScription
    public const string Pages_Subscriptions = "Pages.Subscriptions";
    public const string Pages_Subscriptions_Manager = "Pages.Subscriptions.Manager";

    //Reservation
    public const string Pages_Reservations = "Pages.Reservations";
    public const string Pages_Reservations_ModifyAll= "Pages.Reservations.ModifyAll";
    public const string Pages_Reservations_ViewAll = "Pages.Reservations.ViewAll";
    public const string Pages_Reservations_ModifyOwn = "Pages.Reservations.ModifyOwn";

    //ParkingSession
    public const string Pages_ParkingSessions = "Pages.ParkingSessions";
    public const string Pages_ParkingSessions_ViewAll = "Pages.ParkingSessions.ViewAll";
    public const string Pages_ParkingSessions_ViewOwn = "Pages.ParkingSessions.ViewOwn";

    //ParkingSessionImage
    public const string Pages_ParkingSessions_Image = "Pages.ParkingSessionsImages";
    public const string Pages_ParkingSession_Image_ViewOwn = "Pages.ParkingSessionsImages.ViewOwn";
    public const string Pages_ParkingSession_Image_ViewAll = "Pages.ParkingSessionsImages.ViewAll";



}
