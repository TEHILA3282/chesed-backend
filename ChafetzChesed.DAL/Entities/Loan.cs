using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChafetzChesed.DAL.Entities
{
    public class Loan
    {
        [Key]
        public int ID { get; set; }

        [Required, MaxLength(20)]
        public string ClientID { get; set; } = string.Empty;

        [Required]
        public int LoanTypeID { get; set; }

        [Required]
        public DateTime LoanDate { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public int InstallmentsCount { get; set; }

        [MaxLength(200)]
        public string Purpose { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? PurposeDetails { get; set; }
        public Registration? Client { get; set; }
        public LoanType? LoanType { get; set; }
        public int InstitutionId { get; set; }
        public ICollection<LoanGuarantor> Guarantors { get; set; } = new List<LoanGuarantor>();
        public bool IsDeleted { get; set; }        
        public DateTime UpdatedAt { get; set; }
    }
}
