namespace ChafetzChesed.DAL.Entities
{
    public class Deposit
    {
        public int ID { get; set; }
        public int InstitutionId { get; set; }
        public string ClientID { get; set; } = string.Empty;
        public DateTime DepositDate { get; set; }
        public int DepositTypeID { get; set; }
        public decimal Amount { get; set; }
        public string? PurposeDetails { get; set; }
        public bool IsDirectDeposit { get; set; }
        public DateTime? DepositReceivedDate { get; set; }
        public string? PaymentMethod { get; set; }

        public Registration? Client { get; set; }
        public DepositType? DepositType { get; set; }
    }
}
