using System.Linq;
namespace ParkingSystem.Helpers;

public static class PlateNumberNormalizer
{
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
}
