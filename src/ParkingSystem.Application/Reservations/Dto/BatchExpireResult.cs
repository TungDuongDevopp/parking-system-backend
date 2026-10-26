using System;
using System.Collections.Generic;

namespace ParkingSystem.Reservations.Dto;

public class ExpireReservationResult
{
    public long ReservationId { get; set; }
    public bool IsSuccess { get; set; }
    public bool IsSkipped { get; set; }
    public string Message { get; set; }
    public string ErrorMessage { get; set; }
    public Exception Exception { get; set; }
}

public class BatchExpireResult
{
    public int TotalFound { get; set; }
    public int SuccessCount { get; set; }
    public int SkippedCount { get; set; }
    public int FailedCount { get; set; }
    public List<ExpireReservationResult> Details { get; set; } = new List<ExpireReservationResult>();
}
