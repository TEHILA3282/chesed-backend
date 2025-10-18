using System.ComponentModel.DataAnnotations;

namespace ChafetzChesed.Common.DTOs
{
    public class CreateLoanDto : IValidatableObject
    {
        [Required(ErrorMessage = "חובה לבחור סוג הלוואה")]
        public int LoanTypeId { get; set; }

        [Range(1, double.MaxValue, ErrorMessage = "סכום ההלוואה חייב להיות גדול מאפס")]
        public decimal Amount { get; set; }

        // בהלוואת גישור אפשר להתעלם ולכפות 1 בצד ה-Service
        [Range(1, int.MaxValue, ErrorMessage = "מספר תשלומים חייב להיות לפחות 1")]
        public int PaymentsCount { get; set; } = 1;

        [Required(ErrorMessage = "חובה להזין מטרה")]
        [MaxLength(200, ErrorMessage = "שדה 'מטרה' עד 200 תווים")]
        public string LoanPurpose { get; set; } = string.Empty;

        [Required(ErrorMessage = "חובה להזין פירוט")]
        [MaxLength(2000, ErrorMessage = "שדה 'פירוט' עד 2000 תווים")]
        public string Description { get; set; } = string.Empty;

        public bool IsForApartment { get; set; }
        public bool ApartmentConfirmed { get; set; }

        // אפשר להשאיר רשימה ריקה; האפליקציה תסנן ערכים ריקים בצד ה-Client וה-Service
        public List<GuarantorDto> Guarantors { get; set; } = new();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // כלל תלוי מצב: אם לדירה – חייב אישור
            if (IsForApartment && !ApartmentConfirmed)
            {
                yield return new ValidationResult(
                    "בבחירת 'הלוואה לדירה' יש לאשר את הסעיף המתאים",
                    new[] { nameof(ApartmentConfirmed) }
                );
            }

            // ולידציה עדינה לערבים:
            // אם ערב “לא ריק” (יש לפחות שדה אחד), נדרוש IdNumber + FullName + Phone
            if (Guarantors != null)
            {
                for (int i = 0; i < Guarantors.Count; i++)
                {
                    var g = Guarantors[i];
                    bool any =
                        !string.IsNullOrWhiteSpace(g.IdNumber) ||
                        !string.IsNullOrWhiteSpace(g.FullName) ||
                        !string.IsNullOrWhiteSpace(g.Phone) ||
                        !string.IsNullOrWhiteSpace(g.Occupation) ||
                        !string.IsNullOrWhiteSpace(g.City) ||
                        !string.IsNullOrWhiteSpace(g.Street) ||
                        !string.IsNullOrWhiteSpace(g.HouseNumber) ||
                        !string.IsNullOrWhiteSpace(g.LoanLink) ||
                        !string.IsNullOrWhiteSpace(g.Email);

                    if (any)
                    {
                        if (string.IsNullOrWhiteSpace(g.IdNumber))
                            yield return new ValidationResult("חובה למלא ת\"ז ערב", new[] { $"Guarantors[{i}].IdNumber" });

                        if (string.IsNullOrWhiteSpace(g.FullName))
                            yield return new ValidationResult("חובה למלא שם מלא של ערב", new[] { $"Guarantors[{i}].FullName" });

                        if (string.IsNullOrWhiteSpace(g.Phone))
                            yield return new ValidationResult("חובה למלא טלפון ערב", new[] { $"Guarantors[{i}].Phone" });
                    }
                }
            }
        }
    }
}
