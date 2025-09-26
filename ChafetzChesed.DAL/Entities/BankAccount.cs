namespace ChafetzChesed.DAL.Entities
{
    public class BankAccount
    {
        public int Id { get; set; }
        public int InstitutionId { get; set; }
        public string RegistrationId { get; set; } 
        public string BankNumber { get; set; }
        public string BranchNumber { get; set; }
        public string AccountNumber { get; set; }
        public string AccountOwnerName { get; set; }
        public bool HasDirectDebit { get; set; }
    }
}
