using System.ComponentModel.DataAnnotations;

namespace ChafetzChesed.DAL.Entities
{
    public class Registration
    {
        public int InstitutionId { get; set; }

        [MaxLength(9)]
        public string ID { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(256)]
        public string Password { get; set; } = string.Empty;

        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(30)]
        public string? PhoneNumber { get; set; }

        [MaxLength(30)]
        public string? LandlineNumber { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [MaxLength(30)]
        public string? PersonalStatus { get; set; }

        [MaxLength(100)]
        public string? Street { get; set; }

        [MaxLength(60)]
        public string? City { get; set; }

        [MaxLength(10)]
        public string? HouseNumber { get; set; }

        [MaxLength(20)]
        public string RegistrationStatus { get; set; } = "ממתין";

        public DateTime? StatusUpdatedAt { get; set; }

        // חדש: תפקיד למנגנון אדמין/יוזר
        [MaxLength(20)]
        public string Role { get; set; } = "User";

        // חדש: ניווט למוסד (בשביל AdminController)
        public Institution? Institution { get; set; }

        // כבר הוספנו קודם
        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}
