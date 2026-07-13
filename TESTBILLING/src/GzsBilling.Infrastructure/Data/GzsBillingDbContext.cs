using GzsBilling.Domain.Entities;
using GzsBilling.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace GzsBilling.Infrastructure.Data;

public class GzsBillingDbContext : DbContext
{
    public GzsBillingDbContext(DbContextOptions<GzsBillingDbContext> options)
        : base(options)
    {
    }

    public DbSet<Tranzaksiya> Tranzaktsiyalar => Set<Tranzaksiya>();
    public DbSet<Stakeholder> Stakeholders => Set<Stakeholder>();
    public DbSet<SverkaLog> SverkaLogs => Set<SverkaLog>();
    public DbSet<DisbursementTarixi> DisbursementTarixi => Set<DisbursementTarixi>();
    public DbSet<Schetfaktura> Schetfakturalar => Set<Schetfaktura>();
    public DbSet<FillingStation> FillingStations => Set<FillingStation>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<User> Users => Set<User>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<Dispenser> Dispensers => Set<Dispenser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tranzaksiya>(entity =>
        {
            entity.ToTable("tranzaktsiyalar");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TotalSum).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(e => e.FillingStationId).IsRequired();
            entity.Property(e => e.DispenserId).IsRequired(false);
            entity.Property(e => e.CardType).HasMaxLength(20).IsRequired();
            entity.Property(e => e.IdempotencyKey).HasMaxLength(100).IsRequired();
            entity.Property(e => e.PaymentId).IsRequired();
            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.HasIndex(e => e.IdempotencyKey).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(t => t.Payment)
                .WithMany(p => p.Tranzaktsiyalar)
                .HasForeignKey(t => t.PaymentId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(t => t.Dispenser)
                .WithMany(d => d.Tranzaktsiyalar)
                .HasForeignKey(t => t.DispenserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Stakeholder>(entity =>
        {
            entity.ToTable("stakeholders");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FillingStationId).IsRequired();
            entity.Property(e => e.PaymentId).IsRequired();
            entity.Property(e => e.BankAccount).HasMaxLength(20).IsRequired();
            entity.Property(e => e.SharePercent).HasColumnType("decimal(5,2)").IsRequired();
            entity.Property(e => e.FullName).HasMaxLength(200).IsRequired();
            entity.HasIndex(e => new { e.FillingStationId, e.PaymentId });

            entity.HasOne(s => s.Payment)
                .WithMany()
                .HasForeignKey(s => s.PaymentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SverkaLog>(entity =>
        {
            entity.ToTable("sverka_logs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ReconciliationDate).IsRequired();
            entity.Property(e => e.IssueType)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();
            entity.Property(e => e.Details).HasColumnType("jsonb");
            entity.Property(e => e.IsResolved).HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.HasIndex(e => e.ReconciliationDate);
        });

        modelBuilder.Entity<DisbursementTarixi>(entity =>
        {
            entity.ToTable("disbursement_tarixi");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(e => e.BankReference).HasMaxLength(100);
            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();
            entity.Property(e => e.SentAt).IsRequired();
            entity.HasOne(e => e.Stakeholder)
                .WithMany(s => s.DisbursementHistory)
                .HasForeignKey(e => e.StakeholderId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Tranzaksiya)
                .WithMany(t => t.DisbursementHistory)
                .HasForeignKey(e => e.TranzaksiyaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Schetfaktura>(entity =>
        {
            entity.ToTable("schetfakturalar");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.InvoiceDate).IsRequired();
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(e => e.SystemCommission).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(e => e.NetDistributionAmount).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(e => e.CalculationJson).HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.IsAuthorized).HasDefaultValue(false);
            entity.Property(e => e.IsPaid).HasDefaultValue(false);
            entity.HasIndex(e => e.InvoiceDate);
        });

        modelBuilder.Entity<FillingStation>(entity =>
        {
            entity.ToTable("filling_stations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.Region).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.HasIndex(e => e.Name);
        });

        modelBuilder.Entity<Dispenser>(entity =>
        {
            entity.ToTable("dispensers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.FuelType).HasMaxLength(20).IsRequired().HasDefaultValue("AI-92");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.HasOne(d => d.FillingStation)
                .WithMany(f => f.Dispensers)
                .HasForeignKey(d => d.FillingStationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.FillingStationId);
        });

        modelBuilder.Entity<SystemSetting>(entity =>
        {
            entity.ToTable("system_settings");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Key).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Value).HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.Category).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.UpdatedAt).IsRequired();
            entity.Property(e => e.UpdatedBy).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => e.Key).IsUnique();
            entity.HasIndex(e => e.Category);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).HasMaxLength(50).IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(200).IsRequired();
            entity.Property(e => e.FullName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Role).HasMaxLength(20).IsRequired().HasDefaultValue("manager");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.HasIndex(e => e.Username).IsUnique();
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("payments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.ApiToken).HasMaxLength(500);
            entity.Property(e => e.SslCertificateThumbprint).HasMaxLength(100);
            entity.Property(e => e.SslCertificatePfxBase64).HasColumnType("text");
            entity.Property(e => e.WhiteIpAddresses).HasMaxLength(500);
            entity.HasIndex(e => e.Name).IsUnique();
        });
    }

    public static GzsBillingDbContext CreateReadOnlyContext(string readConnectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<GzsBillingDbContext>();
        optionsBuilder.UseNpgsql(readConnectionString);
        return new GzsBillingDbContext(optionsBuilder.Options);
    }
}
