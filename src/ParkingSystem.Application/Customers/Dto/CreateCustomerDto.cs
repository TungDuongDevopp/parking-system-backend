
using ParkingSystem.Validation;
using System.ComponentModel.DataAnnotations;

namespace ParkingSystem.Customers.Dto
{
    public class CreateCustomerDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
       
        [Required]
        [StringLength(20)]
        [PhoneNumber]
        public string PhoneNumber { get; set; }

        [StringLength(255)]
        [Email]
        public string? Email { get; set; }
    }
}
