using System.Linq;
using System.Text.RegularExpressions;
using ParkingSystem.Entities.Enums;
using ParkingSystem.Exceptions;

namespace ParkingSystem.Helpers;

public static class PlateNumberNormalizer
{
    // Biển số xe Việt Nam tổng quát:
    // 2 chữ số mã địa phương (11-99) + sê-ri đăng ký + 4 hoặc 5 chữ số thứ tự (hỗ trợ dấu chấm/gạch nối)
    private static readonly Regex VietnamesePlateRegex = new Regex(
        @"^[1-9][0-9][-]?([A-Z]{1,2}|[A-Z][0-9]|M[DĐ][0-9]?)[- ]?([0-9]{4,5}|[0-9]{3}[.-][0-9]{2}|[0-9]{2}[.-][0-9]{2})$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Biển số Ô tô: sê-ri là 1 hoặc 2 chữ cái (ví dụ: 30A, 51LD, 29KT, 43AA)
    private static readonly Regex CarPlateRegex = new Regex(
        @"^[1-9][0-9][-]?([A-Z]{1,2})[- ]?([0-9]{4,5}|[0-9]{3}[.-][0-9]{2}|[0-9]{2}[.-][0-9]{2})$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Biển số Xe máy / Mô tô / Xe máy điện: sê-ri gồm chữ+số (B1, C2) hoặc 2 chữ cái (<50cc: AA, AB) hoặc MĐ/MD
    private static readonly Regex MotorbikePlateRegex = new Regex(
        @"^[1-9][0-9][-]?([A-Z][0-9]|[A-Z]{2}|M[DĐ][0-9]?)[- ]?([0-9]{4,5}|[0-9]{3}[.-][0-9]{2}|[0-9]{2}[.-][0-9]{2})$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static string Normalize(string plateNumber)
    {
        if (string.IsNullOrWhiteSpace(plateNumber))
            return null;

        return new string(
            plateNumber
                .Where(c => !char.IsWhiteSpace(c))
                .ToArray()
        ).ToUpperInvariant();
    }

    public static bool IsValid(string plateNumber, VehicleType? vehicleType = null)
    {
        if (string.IsNullOrWhiteSpace(plateNumber))
            return false;

        var normalized = Normalize(plateNumber);

        if (!VietnamesePlateRegex.IsMatch(normalized))
            return false;

        if (vehicleType.HasValue)
        {
            switch (vehicleType.Value)
            {
                case VehicleType.Car:
                    return CarPlateRegex.IsMatch(normalized);

                case VehicleType.Motorcycle:
                case VehicleType.ElectricMotorcycle:
                    return MotorbikePlateRegex.IsMatch(normalized);

                case VehicleType.Bicycle:
                case VehicleType.ElectricBicycle:
                    return true;
            }
        }

        return true;
    }

    public static bool IsValidPlateNumber(string plateNumber)
    {
        return IsValid(plateNumber, null);
    }

    public static void Validate(string plateNumber, VehicleType? vehicleType = null)
    {
        if (string.IsNullOrWhiteSpace(plateNumber))
        {
            throw new BusinessRuleException("Biển số xe không được để trống.");
        }

        var normalized = Normalize(plateNumber);

        if (!VietnamesePlateRegex.IsMatch(normalized))
        {
            throw new BusinessRuleException($"Biển số xe '{plateNumber}' không đúng định dạng biển số xe Việt Nam.");
        }

        if (vehicleType.HasValue)
        {
            if (vehicleType.Value == VehicleType.Car && !CarPlateRegex.IsMatch(normalized))
            {
                throw new BusinessRuleException($"Biển số xe '{plateNumber}' không phù hợp với loại phương tiện Ô tô.");
            }

            if ((vehicleType.Value == VehicleType.Motorcycle || vehicleType.Value == VehicleType.ElectricMotorcycle)
                && !MotorbikePlateRegex.IsMatch(normalized))
            {
                throw new BusinessRuleException($"Biển số xe '{plateNumber}' không phù hợp với loại phương tiện Xe máy / Xe máy điện.");
            }
        }
    }
}
