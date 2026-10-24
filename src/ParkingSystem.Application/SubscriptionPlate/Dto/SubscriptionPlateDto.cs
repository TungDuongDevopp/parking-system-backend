

using Abp.Application.Services.Dto;
using ParkingSystem.Entities.Enums;

namespace ParkingSystem.SubscriptionPlate.Dto;

public class SubscriptionPlateDto : EntityDto<long>
{ 
    public long SubscriptionId { get; set; }
    public string PlateNumber { get; set; }
    public VehicleType VehicleType { get; set; }
    public string VehicleTypeName => VehicleType.ToString();
}
