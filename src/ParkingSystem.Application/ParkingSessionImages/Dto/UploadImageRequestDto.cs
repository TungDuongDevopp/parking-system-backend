
using Microsoft.AspNetCore.Http;
using ParkingSystem.Validation;
using System.ComponentModel.DataAnnotations;

namespace ParkingSystem.ParkingSessionImages.Dto;

public class UploadImageRequestDto
{
    [GreaterThanZero]
    public long ParkingSessionId { get; set; }

    [Required]
    public IFormFile File { get; set; }
}
