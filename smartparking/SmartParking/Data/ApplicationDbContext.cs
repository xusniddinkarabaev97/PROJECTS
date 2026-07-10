using Microsoft.EntityFrameworkCore;
using SmartParking.Models;

namespace SmartParking.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Company> Companies { get; set; }
        public DbSet<Plan> Plans { get; set; }
        public DbSet<Station> Stations { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<SharePercent> SharePercents { get; set; }
        public DbSet<TransactionShare> TransactionShares { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<DahuaDevice> DahuaDevices { get; set; }
        public DbSet<DahuaEvent> DahuaEvents { get; set; }
        public DbSet<ParkingSession> ParkingSessions { get; set; }
        public DbSet<VehicleList> VehicleLists { get; set; }
        public DbSet<DahuaSettings> DahuaSettings { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Company>()
                .HasIndex(c => c.Email)
                .IsUnique();
            modelBuilder.Entity<Company>()
                .HasIndex(c => c.Inn)
                .IsUnique();
            modelBuilder.Entity<Station>().ToTable("stations");

            modelBuilder.Entity<Plan>()
           .HasMany(p => p.SharePercents)
           .WithOne(sp => sp.Plan)
           .HasForeignKey(sp => sp.PlanId);

            modelBuilder.Entity<SharePercent>()
                .HasOne(sp => sp.Plan)
                .WithMany(p => p.SharePercents)
                .HasForeignKey(sp => sp.PlanId);

            modelBuilder.Entity<TransactionShare>()
                .HasOne(ts => ts.Transaction)
                .WithMany() // Adjust this if you have navigation in Transaction model
                .HasForeignKey(ts => ts.TransactionId);

            modelBuilder.Entity<TransactionShare>()
                .HasOne(ts => ts.SharePercent)
                .WithMany()
                .HasForeignKey(ts => ts.SharePercentId);

            modelBuilder.Entity<TransactionShare>()
                .HasOne(ts => ts.Plan)
                .WithMany()
                .HasForeignKey(ts => ts.PlanId);

            // Dahua integration relationships
            modelBuilder.Entity<ParkingSession>()
                .HasOne(s => s.EntryEvent)
                .WithMany()
                .HasForeignKey(s => s.EntryEventId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ParkingSession>()
                .HasOne(s => s.ExitEvent)
                .WithMany()
                .HasForeignKey(s => s.ExitEventId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<DahuaEvent>()
                .HasOne(e => e.DahuaDevice)
                .WithMany()
                .HasForeignKey(e => e.DahuaDeviceId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<DahuaEvent>()
                .HasOne(e => e.ParkingSession)
                .WithMany()
                .HasForeignKey(e => e.ParkingSessionId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<DahuaDevice>()
                .HasOne(d => d.Company)
                .WithMany()
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DahuaDevice>()
                .HasOne(d => d.Station)
                .WithMany()
                .HasForeignKey(d => d.StationId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<DahuaSettings>()
                .HasOne(s => s.Company)
                .WithMany()
                .HasForeignKey(s => s.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VehicleList>()
                .HasOne(v => v.Company)
                .WithMany()
                .HasForeignKey(v => v.CompanyId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ParkingSession>()
                .HasOne(s => s.Client)
                .WithMany()
                .HasForeignKey(s => s.ClientId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ParkingSession>()
                .HasOne(s => s.Transaction)
                .WithMany()
                .HasForeignKey(s => s.TransactionId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ParkingSession>()
                .HasOne(s => s.Device)
                .WithMany()
                .HasForeignKey(s => s.DeviceId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ParkingSession>()
                .HasOne(s => s.Station)
                .WithMany()
                .HasForeignKey(s => s.StationId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
