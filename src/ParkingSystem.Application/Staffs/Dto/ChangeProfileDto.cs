

using Abp.Application.Services.Dto;
using ParkingSystem.Validation;
using System;
using System.ComponentModel.DataAnnotations;

namespace ParkingSystem.Staffs.Dto;

public class ChangeProfileDto
{
    [StringLength(20)]
    [Phone]
    public string? PhoneNumber { get; set; }

    [StringLength(255)]
    [EmailAddress]
    public string? Email { get; set; }

    public bool? Gender { get; set; }

    [NotFuture(ErrorMessage = "Date of birth cannot be in the future.")]
    public DateTime? DateOfBirth { get; set; }

    [StringLength(1023)]
    public string? Address { get; set; }

}
