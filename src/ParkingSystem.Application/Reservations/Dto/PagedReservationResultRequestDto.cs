


using Abp.Application.Services.Dto;
using ParkingSystem.Entities.Enums;
using ParkingSystem.Validation;
using System;
using System.ComponentModel.DataAnnotations;

namespace ParkingSystem.Reservations.Dto;

public class PagedReservationResultRequestDto :PagedResultRequestDto, ISortedResultRequest
{
    public string Keyword { get; set; }
    public string Sorting { get; set; }
    public string CustomerName { get; set; }
    public string ParkingAreaCode { get; set; }
    public string ParkingSpotCode { get; set; }

    [GreaterThanZero]
    public long? CustomerId { get; set; }

    [GreaterThanZero]
    public long? ParkingSpotId { get; set; }

    [GreaterThanZero]
    public long? ParkingAreaId { get; set; }

    public DateTime? ReservedAtFrom { get; set; }

    public DateTime? ReservedAtTo { get; set; }

    public DateTime? ExpireAtFrom { get; set; }
    public DateTime? ExpireAtTo { get; set; }

    [EnumDataType (typeof(ReservationStatus))]
    public ReservationStatus? Status { get; set; }
}
