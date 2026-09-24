using AutoMapper;
using ParkingSystem.Entities;

namespace ParkingSystem.Subscriptions.Dto;

public class SubscriptionMapProfile : Profile
{
    public SubscriptionMapProfile()
    {
        CreateMap<Subscription, SubscriptionDto>();
        CreateMap<CreateSubscriptionDto, Subscription>();
   
    }
}
