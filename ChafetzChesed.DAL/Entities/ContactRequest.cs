using System;

namespace ChafetzChesed.DAL.Entities
{
    public class ContactRequest
    {
        public int ID { get; set; }
        public int InstitutionId { get; set; }

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Institution? Institution { get; set; }
    }
}
