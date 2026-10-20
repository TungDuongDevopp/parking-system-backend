

using AutoMapper;
using ParkingSystem.Entities;

namespace ParkingSystem.ParkingSpots.Dto;

public class ParkingSpotMapProfile: Profile
{
    public ParkingSpotMapProfile()
    {
        CreateMap<ParkingSpot, ParkingSpotDto>()
    .ForMember(
        d => d.ParkingAreaCode,
        o => o.MapFrom(s => s.ParkingArea.ParkingCode))
    .ForMember(
        d => d.ParkingAreaName,
        o => o.MapFrom(s => s.ParkingArea.Name));
        CreateMap<CreateParkingSpotDto, ParkingSpot>();
        CreateMap<UpdateParkingSpotDto, ParkingSpot>()
             .ForAllMembers(opt =>
        opt.Condition((src, dest, srcMember) =>
            srcMember != null && !(srcMember is string s && string.IsNullOrWhiteSpace(s))));
        CreateMap<ChangeStatusDto, ParkingSpot>();
    }
}
