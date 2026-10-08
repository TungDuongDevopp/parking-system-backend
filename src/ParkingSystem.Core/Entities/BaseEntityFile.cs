
using Abp.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace ParkingSystem.Entities;

 public abstract class BaseEntityFile : Entity<long>
{
    [Required]
    public string StorageKey { get; set; }
    [Required]
    public string FileName { get; set; }
    [Required]
    public string ContentType { get; set; }
    [Required]
    public string Extension { get; set; }
    public long FileSize { get; set; }
}
