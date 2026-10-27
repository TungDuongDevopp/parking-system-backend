

using AutoMapper;
using ParkingSystem.Entities;

namespace ParkingSystem.Payments.Dto;

public class PaymentMapProfile:Profile
{
    public PaymentMapProfile()
    {
        CreateMap<Payment,PaymentDto>();
    }
}
