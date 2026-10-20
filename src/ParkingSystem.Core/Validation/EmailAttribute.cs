

using Abp.Collections.Extensions;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace ParkingSystem.Validation;

public class EmailAttribute: ValidationAttribute
{
    public const string EmailRegex = @"^\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$";
    public override bool IsValid(object value)
    {
        // null or empty = field not provided in Partial Update → skip validation
        if (value == null) return true;
        string stringValue = value as string;
        if (string.IsNullOrWhiteSpace(stringValue)) return true;

        var regex = new Regex(EmailRegex);
        return regex.IsMatch(stringValue);
    }
    public override string FormatErrorMessage(string name)
    {
        return $"{name} is not a valid email.";
    }
}
