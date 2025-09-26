namespace ChafetzChesed.DAL.Entities
{  public class AuditLog
    {
        public int ID { get; set; }
        public int InstitutionId { get; set; }
        public string Entity { get; set; } = null!;
        public string EntityId { get; set; } = null!;
        public DateTime ChangedAt { get; set; }
        public string ChangedBy { get; set; } = null!;
        public string ChangesJson { get; set; } = "";
    }

}
