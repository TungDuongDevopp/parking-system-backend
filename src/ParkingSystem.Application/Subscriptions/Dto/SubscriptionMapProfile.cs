using AutoMapper;
using ParkingSystem.Entities;

namespace ParkingSystem.Subscriptions.Dto;

public class SubscriptionMapProfile : Profile
{
    public SubscriptionMapProfile()
    {
        CreateMap<Subscription, SubscriptionDto>()
           .ForMember(d => d.Duration,
               opt => opt.MapFrom(s => s.Quotation.Duration))
           .ForMember(d => d.DurationUnit,
               opt => opt.MapFrom(s => s.Quotation.DurationUnit))
           .ForMember(d => d.Price,
               opt => opt.MapFrom(s => s.Quotation.Price))
           .ForMember(d => d.VehicleType,
               opt => opt.MapFrom(s => s.Quotation.VehicleType));
        CreateMap<CreateSubscriptionDto, Subscription>();
   
    }
}
