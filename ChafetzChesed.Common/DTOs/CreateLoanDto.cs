namespace ChafetzChesed.Common.DTOs
{
    public class CreateLoanDto
    {
        public int LoanTypeId { get; set; }
        public decimal Amount { get; set; }
        public int PaymentsCount { get; set; } = 1;
        public string LoanPurpose { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsForApartment { get; set; }
        public bool ApartmentConfirmed { get; set; }
        public List<GuarantorDto> Guarantors { get; set; } = new();
    }
}