

using AutoMapper;
using ParkingSystem.Entities;

namespace ParkingSystem.SubscriptionPlate.Dto;

public class SubscriptionPlateMapProfile : Profile
{
    public SubscriptionPlateMapProfile()
    {
        CreateMap<AddPlateDto, SubscriptionAllowedPlate>();
        CreateMap<ChangePlateDto, SubscriptionAllowedPlate>();
        CreateMap<SubscriptionAllowedPlate, SubscriptionPlateDto>()
            .ForMember(x=>x.VehicleType,
            opt=>opt.MapFrom(s=>s.Subscription.Quotation.VehicleType));
    }
}
