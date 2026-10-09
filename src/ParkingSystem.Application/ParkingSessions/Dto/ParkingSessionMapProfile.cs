

using Abp.AutoMapper;
using AutoMapper;
using ParkingSystem.Entities;

namespace ParkingSystem.ParkingSessions.Dto;

public class ParkingSessionMapProfile : Profile
{
    public ParkingSessionMapProfile()
    {
        CreateMap<ParkingSession, ParkingSessionDto>()
            .ForMember(x=>x.ParkingAreaCode,
            o=>o.MapFrom(p=>p.ParkingArea.ParkingCode))
            .ForMember(x=>x.ParkingSpotCode,
            o=>o.MapFrom(p=>p.ParkingSpot!=null ? p.ParkingSpot.SpotCode : null));
        CreateMap<ParkingSessionCheckInDto, ParkingSession>();
        CreateMap<ParkingSessionCheckOutDto, ParkingSession>();

    }
}
