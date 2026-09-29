

using ParkingSystem.Validation;
using System;
using System.ComponentModel.DataAnnotations;

namespace ParkingSystem.Reservations.Dto;
public class CreateReservationDto
{
    [Required]
    [GreaterThanZero]
    public long ParkingAreaId { get; set; }
    
    [GreaterThanZero]
    public long? ParkingSpotId { get; set; }

    [Required]
    [NotPast]
    public DateTime ReservedAt { get; set; }
}
