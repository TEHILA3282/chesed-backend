namespace ChafetzChesed.Common.DTOs
{
    public class LoginRequest
    {
        public string Identifier { get; set; }     
        public string Password { get; set; }
        public int InstitutionId { get; set; }
    }
}
