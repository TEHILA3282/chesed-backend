// DAL/Data/AppDbContext.cs
using Microsoft.EntityFrameworkCore;
using ChafetzChesed.DAL.Entities;

namespace ChafetzChesed.DAL.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Registration> Registrations { get; set; }
        public DbSet<Institution> Institutions { get; set; }
        public DbSet<DepositType> DepositTypes { get; set; }
        public DbSet<LoanType> LoanTypes { get; set; }
        public DbSet<Loan> Loans { get; set; }
        public DbSet<Deposit> Deposits { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<AccountAction> AccountActions { get; set; }
        public DbSet<BankAccount> BankAccounts { get; set; }
        public DbSet<LoanGuarantor> LoanGuarantors { get; set; }
        public DbSet<FreezeRequest> FreezeRequests { get; set; }
        public DbSet<DepositWithdrawRequest> DepositWithdrawRequests { get; set; }
        public DbSet<SearchIndexItem> SearchIndexItem { get; set; }
        public DbSet<ContactRequest> ContactRequests { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            /* ===== Tables ===== */
            modelBuilder.Entity<Registration>().ToTable("Registration");
            modelBuilder.Entity<Institution>().ToTable("Institutions");
            modelBuilder.Entity<Message>().ToTable("Messages");
            modelBuilder.Entity<FreezeRequest>().ToTable("FreezeRequests");
            modelBuilder.Entity<LoanGuarantor>().ToTable("LoanGuarantors");
            modelBuilder.Entity<AuditLog>().ToTable("AuditLog");

            /* ===== Registration ===== */
            modelBuilder.Entity<Registration>(e =>
            {
                // PK מרוכב: (InstitutionId, ID)
                e.HasKey(r => new { r.InstitutionId, r.ID });

                // אינדקס אימייל ייחודי פר-מוסד (מדלגים על 'נדחה')
                e.HasIndex(r => new { r.InstitutionId, r.Email })
                 .IsUnique()
                 .HasFilter("[Email] IS NOT NULL AND [RegistrationStatus] <> N'נדחה'");

                // אילוצי עמודות עיקריים
                e.Property(r => r.ID).HasMaxLength(9).IsRequired();
                e.Property(r => r.Email).HasMaxLength(100).IsRequired();
                e.Property(r => r.RegistrationStatus).HasMaxLength(20);

                // קשר למוסד
                e.HasOne(r => r.Institution)
                 .WithMany()
                 .HasForeignKey(r => r.InstitutionId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            /* ===== Message -> Registration  ( (InstitutionId, Zeout) => (InstitutionId, ID) ) ===== */
            modelBuilder.Entity<Message>(entity =>
            {
                entity.Property(m => m.InstitutionId).IsRequired();
                entity.Property(m => m.Zeout).HasMaxLength(9).IsRequired();
                entity.Property(m => m.Perut).IsRequired();
                entity.Property(m => m.CreatedAt).HasColumnType("datetime2(0)");

                entity.HasOne(m => m.Registration)
                      .WithMany(r => r.Messages)
                      .HasForeignKey(m => new { m.InstitutionId, m.Zeout })
                      .HasPrincipalKey(r => new { r.InstitutionId, r.ID })
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(m => new { m.InstitutionId, m.Zeout, m.CreatedAt });
            });

            /* ===== LoanType / Loans ===== */
            modelBuilder.Entity<Loan>(entity =>
            {
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");

                entity.HasOne(e => e.LoanType)
                      .WithMany()
                      .HasForeignKey(e => e.LoanTypeID)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Client)
                      .WithMany()
                      .HasForeignKey(e => new { e.InstitutionId, e.ClientID })
                      .HasPrincipalKey(r => new { r.InstitutionId, r.ID })
                      .OnDelete(DeleteBehavior.Restrict);
            });

            /* ===== FreezeRequests -> Registration (by (InstitutionId, ClientID)) ===== */
            modelBuilder.Entity<FreezeRequest>(entity =>
            {
                entity.HasKey(f => f.ID);
                entity.Property(f => f.ID).UseIdentityColumn();

                entity.Property(f => f.ClientID).HasMaxLength(9).IsRequired();
                entity.Property(f => f.InstitutionId).IsRequired();
                entity.Property(f => f.RequestType).HasMaxLength(20).IsRequired();
                entity.Property(f => f.Reason).HasMaxLength(1000).IsRequired();
                entity.Property(f => f.Acknowledged).HasDefaultValue(false);
                entity.Property(f => f.CreatedAt)
                      .HasColumnType("datetime2(0)")
                      .HasDefaultValueSql("SYSUTCDATETIME()");

                entity.HasOne(f => f.Client)
                      .WithMany()
                      .HasForeignKey(f => new { f.InstitutionId, f.ClientID })
                      .HasPrincipalKey(r => new { r.InstitutionId, r.ID })
                      .OnDelete(DeleteBehavior.Restrict);

                entity.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_FreezeRequests_RequestType",
                        "[RequestType] IN (N'loan', N'deposit')");
                });
            });

            /* ===== DepositWithdrawRequests -> Registration (ללא Navigation במחלקה) ===== */
            modelBuilder.Entity<DepositWithdrawRequest>(entity =>
            {
                entity.HasOne<Registration>()
                      .WithMany()
                      .HasForeignKey(d => new { d.InstitutionId, d.ClientID })
                      .HasPrincipalKey(r => new { r.InstitutionId, r.ID })
                      .OnDelete(DeleteBehavior.Restrict);
            });

            /* ===== DepositTypes ===== */
            modelBuilder.Entity<DepositType>(t =>
            {
                t.HasIndex(x => x.Name).IsUnique();
                t.Property(x => x.Name).HasMaxLength(100).IsRequired();
            });

            /* ===== Deposits -> Registration + DepositType(Name) ===== */
            modelBuilder.Entity<Deposit>(entity =>
            {
                // קשר ל-Registration
                entity.HasOne(d => d.Client)
                      .WithMany()
                      .HasForeignKey(d => new { d.InstitutionId, d.ClientID })
                      .HasPrincipalKey(r => new { r.InstitutionId, r.ID })
                      .OnDelete(DeleteBehavior.Restrict);

                // עמודת FK מפורשת
                entity.Property(d => d.DepositTypeId)
                      .HasColumnName("DepositTypeId")
                      .HasMaxLength(100)
                      .IsRequired();

                // קשר ל-DepositTypes לפי Name
                entity.HasOne(d => d.DepositType)
                      .WithMany()
                      .HasPrincipalKey(t => t.Name)        // principal key = Name
                      .HasForeignKey(d => d.DepositTypeId) // FK בטבלת Deposits הוא השם
                      .OnDelete(DeleteBehavior.Restrict)
                      .HasConstraintName("FK_Deposits_DepositTypes_Name");

                // התעלמות משדות Shadow ישנים אם קיימים
                entity.Ignore("DepositTypeID");
                entity.Ignore("DepositTypeId1");
            });

            /* ===== BankAccounts -> Registration (ללא Navigation במחלקה) ===== */
            modelBuilder.Entity<BankAccount>(entity =>
            {
                entity.HasOne<Registration>()
                      .WithMany()
                      .HasForeignKey(b => new { b.InstitutionId, b.RegistrationId })
                      .HasPrincipalKey(r => new { r.InstitutionId, r.ID })
                      .OnDelete(DeleteBehavior.Restrict);
            });


            /* ===== AuditLog ===== */
            modelBuilder.Entity<AuditLog>(e =>
            {
                e.HasKey(x => x.ID);
                e.Property(x => x.InstitutionId).IsRequired();
                e.Property(x => x.Entity).HasMaxLength(64).IsRequired();
                e.Property(x => x.EntityId).HasMaxLength(64).IsRequired();
                e.Property(x => x.ChangedAt).IsRequired();
                e.Property(x => x.ChangedBy).HasMaxLength(64).IsRequired();
                e.Property(x => x.ChangesJson).IsRequired();
                e.HasIndex(x => new { x.InstitutionId, x.Entity, x.EntityId, x.ChangedAt });
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
