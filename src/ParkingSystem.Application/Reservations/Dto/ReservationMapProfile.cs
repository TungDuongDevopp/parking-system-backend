

using AutoMapper;

namespace ParkingSystem.Reservations.Dto;

public class ReservationMapProfile : Profile
{
    public ReservationMapProfile()
    {
        CreateMap<Reservation,ReservationDto>().ForMember(
        dest => dest.EndTime,
        opt => opt.MapFrom(src => src.ExpireAt.AddMinutes(-15))
    );
        CreateMap<CreateReservationDto, Reservation>();
    }
}
