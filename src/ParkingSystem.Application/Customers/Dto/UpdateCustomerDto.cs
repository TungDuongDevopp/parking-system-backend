

using Abp.Application.Services.Dto;
using ParkingSystem.Validation;
using System.ComponentModel.DataAnnotations;

namespace ParkingSystem.Customers.Dto;

public class UpdateCustomerDto: EntityDto<long>
{

    [StringLength(100)]
    public string? Name { get; set; }

    [StringLength(20)]
    [PhoneNumber]
    public string? PhoneNumber { get; set; }

    [StringLength(100)]
    [Email]
    public string? Email { get; set; }
}
