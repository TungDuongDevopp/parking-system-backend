
using ParkingSystem.Validation;
using System.ComponentModel.DataAnnotations;

namespace ParkingSystem.Customers.Dto
{
    public class CreateCustomerDto
    {
        [Required]
        [StringLength(150)]
        public string Name { get; set; }
       
        [StringLength(20)]
        [PhoneNumber]
        public string? PhoneNumber { get; set; }

        [Required]
        [StringLength(256)]
        [Email]
        public string Email { get; set; }
    }
}
