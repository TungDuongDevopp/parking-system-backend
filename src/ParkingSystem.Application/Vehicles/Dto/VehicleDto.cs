

using Abp.Application.Services.Dto;
using ParkingSystem.Entities.Enums;
using System;


namespace ParkingSystem.Vehicles.Dto;

public class VehicleDto : EntityDto<long>
{

    public string VehicleCode { get; set; }

    public VehicleType VehicleType { get; set; }

    public string VehicleTypeName => VehicleType.ToString();
    public string LicensePlate { get; set; }

    public string Brand { get; set; }
    public string Color { get; set; }
    public DateTime CreationTime { get; set; }
    public bool IsDeleted { get; set; }

    public long CustomerId { get; set; }
    public string CustomerName { get; set; }
}
