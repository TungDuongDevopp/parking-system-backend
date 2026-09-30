

using AutoMapper;

namespace ParkingSystem.Reservations.Dto;

public class ReservationMapProfile : Profile
{
    public ReservationMapProfile()
    {
        CreateMap<Reservation,ReservationDto>()
              .ForMember(
        d => d.ParkingAreaCode,
        o => o.MapFrom(s => s.ParkingArea.ParkingCode))
              .ForMember(
        d => d.ParkingAreaName,
        o => o.MapFrom(s => s.ParkingArea.Name))
              .ForMember(
        d => d.ParkingSpotCode,
        o => o.MapFrom(s => s.ParkingSpot != null
            ? s.ParkingSpot.SpotCode
            : null))
            .ForMember(
        dest => dest.EndTime,
        opt => opt.MapFrom(src => src.ExpireAt.AddMinutes(-15))
    )
            .ForMember(
        d => d.VehicleType,
        o => o.MapFrom(s => s.ParkingArea != null ? (Entities.Enums.VehicleType?)s.ParkingArea.VehicleType : null)
    );
        CreateMap<CreateReservationDto, Reservation>();
    }
}
