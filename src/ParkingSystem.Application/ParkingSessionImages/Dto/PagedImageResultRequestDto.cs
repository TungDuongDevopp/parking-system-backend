using Abp.Application.Services.Dto;
using ParkingSystem.Entities.Enums;
using ParkingSystem.Validation;
using System;
using System.ComponentModel.DataAnnotations;

namespace ParkingSystem.ParkingSessionImages.Dto;

public class PagedImageResultRequestDto: PagedResultRequestDto, ISortedResultRequest
{
    public string Keyword { get; set; }
    public string Sorting { get; set; }

    [GreaterThanZero]
    public long? ParkingSessionId { get; set; }

    [EnumDataType(typeof(SessionImageType))]
    public SessionImageType? Type { get; set; }

    [GreaterThanZero]
    public long? MinFileSize { get; set; }
    
    [GreaterThanZero]
    public long? MaxFileSize { get; set; }

    public DateTime? CapturedAt { get; set; }


}
