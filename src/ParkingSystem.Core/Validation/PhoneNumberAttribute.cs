
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace ParkingSystem.Validation;

public class PhoneNumberAttribute: ValidationAttribute
{
    public const string PhoneRegex = @"\A0[35789][0-9]{8}\z";
    public override bool IsValid(object value)
    {
        // null or empty = field not provided in Partial Update → skip validation
        if (value == null) return true;
        string stringValue = value as string;
        if (string.IsNullOrWhiteSpace(stringValue)) return true;

        return Regex.IsMatch(stringValue, PhoneRegex, RegexOptions.CultureInvariant);
    }
    public override string FormatErrorMessage(string name)
    {
        return $"{name} is not a valid phone number.";
    }
}
