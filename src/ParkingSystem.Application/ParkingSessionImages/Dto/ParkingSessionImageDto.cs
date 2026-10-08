
using Abp.Application.Services.Dto;
using ParkingSystem.Entities.Enums;
using System;

namespace ParkingSystem.ParkingSessionImages.Dto;

public class ParkingSessionImageDto : EntityDto<long>
{
    public long ParkingSessionId { get; set; }
    public string TicketCode { get; set; }
    public SessionImageType Type { get; set; }
    public string SessionImageTypeName => Type.ToString();
    public string StorageKey { get; set; }
    public string FileName { get; set; }
    public string ContentType { get; set; }
    public string Extension { get; set; }
    public long FileSize { get; set; }
    public DateTime CapturedAt { get; set; }
}
