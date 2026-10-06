
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace ParkingSystem.Validation;

public class PhoneNumberAttribute: ValidationAttribute
{
    public const string PhoneRegex = @"^0(3|5|7|8|9)\d{8}$";
    public override bool IsValid(object value)
    {
        string stringValue = value as string;
        if (value == null)
        {
            return false;
        }

        var regex = new Regex(PhoneRegex);
        return regex.IsMatch(stringValue);
    }
    public override string FormatErrorMessage(string name)
    {
        return $"{name} is not a valid phone number.";
    }
}
