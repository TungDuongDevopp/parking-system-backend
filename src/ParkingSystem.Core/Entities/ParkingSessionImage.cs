
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using Abp.Timing;
using ParkingSystem.Entities.Enums;
using System;

namespace ParkingSystem.Entities;

public class ParkingSessionImage : BaseEntityFile, ISoftDelete, IHasCreationTime
{
    public long ParkingSessionId { get; set; }

    public ParkingSession ParkingSession { get; set; }

    public SessionImageType Type { get; set; }

    public DateTime CapturedAt { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime CreationTime { get; set; }

    public ParkingSessionImage()
    {
        CreationTime = Clock.Now;
        IsDeleted = false;
    }
}
