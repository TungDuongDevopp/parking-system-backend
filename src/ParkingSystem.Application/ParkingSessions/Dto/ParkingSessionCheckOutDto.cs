using Abp.Application.Services.Dto;
using ParkingSystem.Entities.Enums;

namespace ParkingSystem.ParkingSessions.Dto;

public class ParkingSessionCheckOutDto : EntityDto<long>
{
    public PaymentMethod? PaymentMethod { get; set; }
}
