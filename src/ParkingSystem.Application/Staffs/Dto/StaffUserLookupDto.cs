namespace ParkingSystem.Staffs.Dto;

/// <summary>
/// Represents a user eligible to be assigned a Staff profile.
/// Used by React frontend when creating a new Staff record.
/// </summary>
public class StaffUserLookupDto
{
    public long Id { get; set; }
    public string UserName { get; set; }
    public string FullName { get; set; }
    public string EmailAddress { get; set; }
    public string PhoneNumber { get; set; }
}
