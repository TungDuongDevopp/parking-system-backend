
using Abp.Application.Services.Dto;
using System;

namespace ParkingSystem.Customers.Dto

{
    public class CustomerDto : EntityDto<long>
    {
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public string? Email { get; set; }
    }
}
