

using System.ComponentModel.DataAnnotations;

namespace ParkingSystem.Validation;

public class EmailAttribute: ValidationAttribute
{
    public const string EmailRegex = ValidationHelper.EmailRegex;
    public override bool IsValid(object value)
    {
        // null or empty = field not provided in Partial Update → skip validation
        if (value == null) return true;
        string stringValue = value as string;
        if (string.IsNullOrWhiteSpace(stringValue)) return true;

        return ValidationHelper.IsEmail(stringValue);
    }
    public override string FormatErrorMessage(string name)
    {
        return $"{name} is not a valid email.";
    }
}
