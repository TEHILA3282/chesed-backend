using System.ComponentModel.DataAnnotations;

namespace ChafetzChesed.DAL.Entities
{
    public class Message
    {
        [Key]
        public int Id { get; set; }

        // FK חלק 2 (מתכתב עם Registration.ID)
        [Required]
        [MaxLength(9)]
        public string Zeout { get; set; } = string.Empty;

        // FK חלק 1 (מתכתב עם Registration.InstitutionId)
        [Required]
        public int InstitutionId { get; set; }

        [Required]
        public int Seder { get; set; }

        [Required]
        public string Perut { get; set; } = string.Empty;

        [Required]
        public int Important { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        // הסרנו את [ForeignKey(nameof(Zeout))] – המיפוי יעשה ב-OnModelCreating עם FK מרוכב
        public Registration? Registration { get; set; }
    }
}
