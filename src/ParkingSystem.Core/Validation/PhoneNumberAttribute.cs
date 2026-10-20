
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace ParkingSystem.Validation;

public class PhoneNumberAttribute: ValidationAttribute
{
    public const string PhoneRegex = @"^0(3|5|7|8|9)\d{8}$";
    public override bool IsValid(object value)
    {
        // null or empty = field not provided in Partial Update → skip validation
        if (value == null) return true;
        string stringValue = value as string;
        if (string.IsNullOrWhiteSpace(stringValue)) return true;

        var regex = new Regex(PhoneRegex);
        return regex.IsMatch(stringValue);
    }
    public override string FormatErrorMessage(string name)
    {
        return $"{name} is not a valid phone number.";
    }
}
