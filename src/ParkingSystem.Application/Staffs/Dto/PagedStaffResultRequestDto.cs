

using Abp.Application.Services.Dto;
using ParkingSystem.Entities.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace ParkingSystem.Staffs.Dto;

public class PagedStaffResultRequestDto : PagedResultRequestDto, ISortedResultRequest
{
    public string Keyword { get; set; }
    public string Sorting { get; set; }

    public DateTime? HiredDateFrom { get; set; }
    public DateTime? HiredDateTo { get; set; }

    public DateTime? DateOfBirthFrom { get; set; }
    public DateTime? DateOfBirthTo { get; set; }

    [EnumDataType(typeof(StaffStatus))]
    public StaffStatus? Status { get; set; }
}
