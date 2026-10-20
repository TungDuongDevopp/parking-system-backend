using Abp.Extensions;
using System.Text.RegularExpressions;

namespace ParkingSystem.Validation;

public static class ValidationHelper
{
    public const string EmailRegex = @"\A[\p{L}\p{M}\p{N}!#$%&'*+/=?^_`{|}~-]+(?:\.[\p{L}\p{M}\p{N}!#$%&'*+/=?^_`{|}~-]+)*@(?:[\p{L}\p{N}](?:[\p{L}\p{N}-]{0,61}[\p{L}\p{N}])?\.)+[\p{L}\p{N}](?:[\p{L}\p{N}-]{0,61}[\p{L}\p{N}])?\z";

    public static bool IsEmail(string value)
    {
        if (value.IsNullOrEmpty())
        {
            return false;
        }

        return Regex.IsMatch(value, EmailRegex, RegexOptions.CultureInvariant);
    }
}
