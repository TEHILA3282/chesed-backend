using System.ComponentModel.DataAnnotations;

namespace ChafetzChesed.DAL.Entities
{
    public class Registration
    {
        [Key]
        public string ID { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = null!;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = null!;

        [StringLength(15)]
        public string? PhoneNumber { get; set; }

        [StringLength(15)]
        public string? LandlineNumber { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = null!;

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(20)]
        public string? PersonalStatus { get; set; }

        [StringLength(100)]
        public string? Street { get; set; }

        [StringLength(50)]
        public string? City { get; set; }

        [StringLength(10)]
        public string? HouseNumber { get; set; }

        [Required]
        [StringLength(100)]
        public string? Password { get; set; }

        [StringLength(20)]
        public string RegistrationStatus { get; set; } = "ממתין";

        [DataType(DataType.Date)]
        public DateTime? StatusUpdatedAt { get; set; }

        public int InstitutionId { get; set; }
        public Institution? Institution { get; set; }

        public string Role { get; set; } = "User";
    }
}
