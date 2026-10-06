using Abp.Timing;
using System;
using System.ComponentModel.DataAnnotations;


namespace ParkingSystem.Validation;

public class NotPastAttribute : ValidationAttribute
{
    public override bool IsValid(object value)
    {
        if (value == null)
            return true;

        return value is DateTime dateTimeValue
            && dateTimeValue >= Clock.Now;
    }
    public override string FormatErrorMessage(string name)
    {
        return $"{name} must be a date in the future.";
    }
}
