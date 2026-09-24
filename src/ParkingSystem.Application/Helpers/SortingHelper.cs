
using ParkingSystem.Exceptions;
using System;
using System.Collections.Generic;

namespace ParkingSystem.Helpers
{
    public static class SortingHelper
    {
        public static string ValidateSorting(
            string sorting,
            params string[] allowedFields)
        {
            if (string.IsNullOrWhiteSpace(sorting))
                return null;

            var allowed = new HashSet<string>(
                allowedFields,
                StringComparer.OrdinalIgnoreCase);

            // Hỗ trợ: "Name asc", "CreationTime desc"
            // và nhiều field: "Name asc, CreationTime desc"
            var sortParts = sorting.Split(',');

            foreach (var part in sortParts)
            {
                var tokens = part.Trim()
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (tokens.Length == 0 || tokens.Length > 2)
                    throw new InvalidSortingException("Invalid sorting format.");

                var field = tokens[0];

                if (!allowed.Contains(field))
                    throw new InvalidSortingException(
                        $"Sorting by '{field}' is not allowed.");

                if (tokens.Length == 2 &&
                    !tokens[1].Equals("asc", StringComparison.OrdinalIgnoreCase) &&
                    !tokens[1].Equals("desc", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidSortingException(
                        "Sorting direction must be 'asc' or 'desc'.");
                }
            }

            return sorting;
        }
    }
}
