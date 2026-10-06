
using Abp.Application.Services.Dto;
using ParkingSystem.Validation;
using System;
using System.ComponentModel.DataAnnotations;

namespace ParkingSystem.Staffs.Dto;

public class UpdateStaffDto : EntityDto<long>
{
    
    [StringLength(100)]
    public string? Name { get; set; }

    [StringLength(20)]
    [PhoneNumber]
    public string? PhoneNumber { get; set; }

    [StringLength(255)]
    [Email]
    public string? Email { get; set; }

    public bool? Gender { get; set; } 

    [NotFuture]
    public DateTime? HiredDate { get; set; }

    [NotFuture]
    public DateTime? DateOfBirth { get; set; }

    [StringLength(1023)]
    public string? Address { get; set; }
}
