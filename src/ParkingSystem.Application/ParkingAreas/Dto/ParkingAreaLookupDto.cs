using Abp.Application.Services.Dto;
using ParkingSystem.Entities.Enums;

namespace ParkingSystem.ParkingAreas.Dto;

public class ParkingAreaLookupDto : EntityDto<long>
{
    public string ParkingCode { get; set; }

    public string Name { get; set; }

    public VehicleType VehicleType { get; set; }

    public string VehicleTypeName => VehicleType.ToString();

    public int Capacity { get; set; }

    public ParkingMode ParkingMode { get; set; }

    public string ParkingModeName => ParkingMode.ToString();

    public string Location { get; set; }

    public string Description { get; set; }

    public ParkingAreaStatus Status { get; set; }

    public string ParkingAreaStatusName => Status.ToString();

    public int AvailableCapacity { get; set; }

    public int UnavailableCapacity { get; set; }
}
