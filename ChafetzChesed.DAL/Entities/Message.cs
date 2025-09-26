using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ChafetzChesed.DAL.Entities
{
    public class Message
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Zeout { get; set; } = string.Empty;

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

        [ForeignKey(nameof(Zeout))]
        public Registration? Registration { get; set; }
    }
}

