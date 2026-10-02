
using ParkingSystem.ParkingSpots.Dto;
using System.Collections.Generic;
namespace ParkingSystem.Web.Models.ParkingSpot;


public class ParkingSpotListViewModel
{
    public IReadOnlyList<ParkingSpotLookUpDto> ParkingAreas = new List<ParkingSpotLookUpDto>();
}
