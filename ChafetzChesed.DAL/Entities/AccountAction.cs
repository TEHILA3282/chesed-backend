namespace ChafetzChesed.DAL.Entities
{
    public class AccountAction
    {
        public int Id { get; set; }
        public int InstitutionId { get; set; }
        public string Zeout { get; set; }
        public int Seder { get; set; }
        public string Perut { get; set; } = string.Empty;
        public int Important { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

}
