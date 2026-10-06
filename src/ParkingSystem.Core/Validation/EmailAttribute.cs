

using Abp.Collections.Extensions;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace ParkingSystem.Validation;

public class EmailAttribute: ValidationAttribute
{
    public const string EmailRegex = @"^\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$";
    public override bool IsValid(object value)
    {
        string stringValue = value as string;
        if (value == null)
        {
            return false;
        }
       

        var regex = new Regex(EmailRegex);
        return regex.IsMatch(stringValue);
    }
    public override string FormatErrorMessage(string name)
    {
        return $"{name} is not a valid email.";
    }
}
