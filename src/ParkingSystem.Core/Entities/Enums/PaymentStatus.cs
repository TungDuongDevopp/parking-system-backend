namespace ParkingSystem.Entities.Enums;

public enum PaymentStatus
{
    Pending = 0, // Chưa thanh toán thành công, vẫn còn hiệu lực
    Paid = 1,    // Đã thanh toán đủ
    Failed = 2,  // Bị kết thúc do lỗi nghiệp vụ
    Expired = 3  // Hết thời hạn thanh toán
}
