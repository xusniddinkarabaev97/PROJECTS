using Microsoft.EntityFrameworkCore;
using ODULink.Models;

namespace ODULink.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Company> Companies { get; set; }
        public DbSet<Plan> Plans { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<SharePercent> SharePercents { get; set; }
        public DbSet<TransactionShare> TransactionShares { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<ClickPayment> ClickPayments { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Company>()
                .HasIndex(c => c.Login)
                .IsUnique();
            modelBuilder.Entity<Company>()
                .HasIndex(c => c.Inn)
                .IsUnique();

            modelBuilder.Entity<Department>().ToTable("departments");

            modelBuilder.Entity<Patient>()
                .HasIndex(p => p.ExternalId)
                .IsUnique();

            modelBuilder.Entity<Plan>()
                .HasMany(p => p.SharePercents)
                .WithOne(sp => sp.Plan)
                .HasForeignKey(sp => sp.PlanId);

            modelBuilder.Entity<SharePercent>()
                .HasOne(sp => sp.Plan)
                .WithMany(p => p.SharePercents)
                .HasForeignKey(sp => sp.PlanId);

            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Department)
                .WithMany()
                .HasForeignKey(t => t.DepartmentId);

            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Patient)
                .WithMany(p => p.Transactions)
                .HasForeignKey(t => t.PatientId);

            modelBuilder.Entity<TransactionShare>()
                .HasOne(ts => ts.Transaction)
                .WithMany()
                .HasForeignKey(ts => ts.TransactionId);

            modelBuilder.Entity<TransactionShare>()
                .HasOne(ts => ts.SharePercent)
                .WithMany()
                .HasForeignKey(ts => ts.SharePercentId);

            modelBuilder.Entity<TransactionShare>()
                .HasOne(ts => ts.Plan)
                .WithMany()
                .HasForeignKey(ts => ts.PlanId);

            modelBuilder.Entity<ClickPayment>()
                .HasIndex(p => p.ClickTransId)
                .IsUnique();

            modelBuilder.Entity<ClickPayment>()
                .HasIndex(p => p.MerchantTransId)
                .IsUnique();
        }
    }
}
