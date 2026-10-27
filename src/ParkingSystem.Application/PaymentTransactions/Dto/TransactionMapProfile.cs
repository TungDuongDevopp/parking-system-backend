

using AutoMapper;
using ParkingSystem.Entities;

namespace ParkingSystem.PaymentTransactions.Dto;

public class TransactionMapProfile : Profile
{
    public TransactionMapProfile()
    {
        CreateMap<PaymentTransaction,TransactionDto>();
    }
}
