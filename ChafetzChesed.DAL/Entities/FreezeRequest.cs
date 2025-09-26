using ChafetzChesed.Common.Constants;

namespace ChafetzChesed.DAL.Entities
{
    public class FreezeRequest
    {
        public int ID { get; set; }
        public string ClientID { get; set; } = string.Empty;
        public int InstitutionId { get; set; }
        public string RequestType { get; set; } = FreezeRequestTypeValues.Loan;
        public string Reason { get; set; } = string.Empty;
        public bool Acknowledged { get; set; }
        public DateTime CreatedAt { get; set; }
        public Registration? Client { get; set; }
    }
}
