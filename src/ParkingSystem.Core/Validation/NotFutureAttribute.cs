using Abp.Timing;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkingSystem.Validation
{
    public class NotFutureAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            if (value == null)
                return true;

            return value is DateTime dateTimeValue
                && dateTimeValue <= Clock.Now;
        }
    }
}
