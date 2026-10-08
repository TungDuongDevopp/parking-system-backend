using AutoMapper;
using ParkingSystem.Entities;

namespace ParkingSystem.ParkingSessionImages.Dto;

public class ParkingSessionMapProfile : Profile
{
    public ParkingSessionMapProfile()
    {
        CreateMap<ParkingSessionImage, ParkingSessionImageDto>()
            .ForMember(
            d=>d.TicketCode,
            o=>o.MapFrom(s=>s.ParkingSession.TicketCode)
            );
    }
}
