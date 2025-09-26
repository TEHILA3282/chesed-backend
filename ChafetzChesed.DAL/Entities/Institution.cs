namespace ChafetzChesed.DAL.Entities
{
    public class Institution
    {
        public int InstitutionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Subdomain { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public string? ThemeColor { get; set; }
        public bool IsActive { get; set; }   
        public string? ContactPhone { get; set; }
        public string? AvailabilityText { get; set; }
    }
}
