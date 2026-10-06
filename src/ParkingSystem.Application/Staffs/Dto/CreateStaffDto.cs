

using Abp.AutoMapper;
using ParkingSystem.Entities;
using ParkingSystem.Validation;
using System;
using System.ComponentModel.DataAnnotations;

namespace ParkingSystem.Staffs.Dto;

[AutoMapTo(typeof(Staff))]
public class CreateStaffDto
{
    [Required]
    [GreaterThanZero]
    public long UserId { get; set; }
   
    [Required]
    [StringLength(100)]
    public string Name { get; set; }

    [Required]
    [StringLength(20)]
    [PhoneNumber]
    public string PhoneNumber { get; set; }

    [StringLength(255)]
    [Email]
    public string? Email { get; set; }

    public bool? Gender { get; set; } // true for male, false for female

    [Required]
    [NotFuture]
    public DateTime HiredDate { get; set; }

    [NotFuture]
    public DateTime? DateOfBirth { get; set; }

    [StringLength(1023)]
    public string? Address { get; set; }

}
