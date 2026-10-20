

using AutoMapper;
using ParkingSystem.Entities;

namespace ParkingSystem.ParkingAreas.Dto;

public class ParkingAreaMapProfile: Profile
{
    public ParkingAreaMapProfile()
    {
        CreateMap<ParkingArea, ParkingAreaDto>();

        CreateMap<CreateParkingAreaDto, ParkingArea>();

        CreateMap<UpdateParkingAreaDto, ParkingArea>()
            .ForMember(d => d.VehicleType, opt =>
                opt.PreCondition(s => s.VehicleType.HasValue))
            .ForMember(d => d.Capacity, opt =>
                opt.PreCondition(s => s.Capacity.HasValue))
            .ForMember(d => d.ParkingMode, opt =>
                opt.PreCondition(s => s.ParkingMode.HasValue))
            .ForAllMembers(opt =>
                opt.Condition((src, dest, srcMember) =>
                    srcMember != null && !(srcMember is string s && string.IsNullOrWhiteSpace(s))));
        CreateMap<ChangeStatusDto,ParkingArea>();
    }
}
