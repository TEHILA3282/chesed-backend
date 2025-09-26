namespace ChafetzChesed.Common.DTOs
{
    public class ContactRequestCreateDto
    {
        public int InstitutionId { get; set; } 
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class InstitutionPublicInfoDto
    {
        public int InstitutionId { get; set; }
        public string Phone { get; set; } = string.Empty;
        public string AvailabilityText { get; set; } = string.Empty; 
    }
}