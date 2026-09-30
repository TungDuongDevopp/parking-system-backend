

namespace ParkingSystem.Entities.Enums;

public enum ReservationStatus
{
    Reserved = 1,       // hoặc Reserved
    Completed = 2,     // đã đến và tạo ParkingSession
    Cancelled = 3,     // khách chủ động hủy
    Expired = 4,        // quá 3h
    Invalid = 5        // sai lệch giữa vehicle type và area  

}
