using Abp.Timing;
using System;
using System.ComponentModel.DataAnnotations;


namespace ParkingSystem.Validation
{
    public class NotPastAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            if (value == null)
                return true;

            return value is DateTime dateTimeValue
                && dateTimeValue >= Clock.Now;
        }
    }
}
