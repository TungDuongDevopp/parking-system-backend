
using ParkingSystem.Reservations.Dto;
using System.Collections.Generic;

namespace ParkingSystem.Web.Models.Reservation;

public class ReservationListViewModel
{
    public IReadOnlyList<ReservationCustomerLookUpDto> Customers = new List<ReservationCustomerLookUpDto>();
    public IReadOnlyList<ReservationParkingAreaLookUpDto> ParkingAreas = new List<ReservationParkingAreaLookUpDto>();
    public IReadOnlyList<ReservationParkingSpotLookUpDto> ParkingSpots = new List<ReservationParkingSpotLookUpDto>();


}
