using Microsoft.EntityFrameworkCore;
using PaScan.Models;
using PaScan.Enums;

namespace PaScan.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Course> Courses { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Student> Students { get; set; } = null!;
    public DbSet<Admin> Admins { get; set; } = null!;
    public DbSet<Scanner> Scanners { get; set; } = null!;
    public DbSet<DeviceRequest> DeviceRequests { get; set; } = null!;
    public DbSet<DeviceRequestAccessory> DeviceRequestAccessories { get; set; } = null!;
    public DbSet<Device> Devices { get; set; } = null!;
    public DbSet<DeviceAccessory> DeviceAccessories { get; set; } = null!;
    public DbSet<QRToken> QRTokens { get; set; } = null!;
    public DbSet<RFIDCard> RFIDCards { get; set; } = null!;
    public DbSet<GateScanLog> GateScanLogs { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Global query filters for soft-deleted entities
        modelBuilder.Entity<Course>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<User>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<Student>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<Admin>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<Scanner>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<DeviceRequest>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<DeviceRequestAccessory>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<Device>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<DeviceAccessory>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<QRToken>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<RFIDCard>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<GateScanLog>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<RefreshToken>().HasQueryFilter(e => e.DeletedAt == null);

        // Define string conversion for Enums
        modelBuilder.Entity<User>().Property(e => e.Role).HasConversion<string>();
        modelBuilder.Entity<Scanner>().Property(e => e.ScannerType).HasConversion<string>();
        modelBuilder.Entity<Scanner>().Property(e => e.Status).HasConversion<string>();
        modelBuilder.Entity<DeviceRequest>().Property(e => e.DeviceType).HasConversion<string>();
        modelBuilder.Entity<DeviceRequest>().Property(e => e.Status).HasConversion<string>();
        modelBuilder.Entity<Device>().Property(e => e.DeviceType).HasConversion<string>();
        modelBuilder.Entity<Device>().Property(e => e.Status).HasConversion<string>();
        modelBuilder.Entity<QRToken>().Property(e => e.Status).HasConversion<string>();
        modelBuilder.Entity<QRToken>().Property(e => e.RevocationReason).HasConversion<string>();
        modelBuilder.Entity<RFIDCard>().Property(e => e.Status).HasConversion<string>();
        modelBuilder.Entity<RFIDCard>().Property(e => e.InvalidationReason).HasConversion<string>();
        modelBuilder.Entity<GateScanLog>().Property(e => e.ScanType).HasConversion<string>();

        // ── User ↔ Profile one-to-one relationships ──

        modelBuilder.Entity<Student>()
            .HasOne(s => s.User)
            .WithOne(u => u.Student)
            .HasForeignKey<Student>(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Admin>()
            .HasOne(a => a.User)
            .WithOne(u => u.Admin)
            .HasForeignKey<Admin>(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Scanner>()
            .HasOne(s => s.User)
            .WithOne(u => u.Scanner)
            .HasForeignKey<Scanner>(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Course → Student ──

        modelBuilder.Entity<Student>()
            .HasOne(s => s.Course)
            .WithMany(c => c.Students)
            .HasForeignKey(s => s.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Admin self-referencing (created_by) ──

        modelBuilder.Entity<Admin>()
            .HasOne(a => a.AdminCreator)
            .WithMany()
            .HasForeignKey(a => a.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Admin → Scanner relationships ──

        modelBuilder.Entity<Scanner>()
            .HasOne(s => s.AdminCreator)
            .WithMany(a => a.ScannersCreated)
            .HasForeignKey(s => s.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Scanner>()
            .HasOne(s => s.AdminKeyGenerator)
            .WithMany()
            .HasForeignKey(s => s.ApiKeyGeneratedBy)
            .OnDelete(DeleteBehavior.SetNull);

        // ── DeviceRequest relationships ──

        modelBuilder.Entity<DeviceRequest>()
            .HasOne(d => d.Student)
            .WithMany(s => s.DeviceRequests)
            .HasForeignKey(d => d.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DeviceRequest>()
            .HasOne(d => d.AdminReviewer)
            .WithMany()
            .HasForeignKey(d => d.ReviewedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Device relationships ──

        modelBuilder.Entity<Device>()
            .HasOne(d => d.Student)
            .WithMany(s => s.Devices)
            .HasForeignKey(d => d.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Device>()
            .HasOne(d => d.AdminApprover)
            .WithMany()
            .HasForeignKey(d => d.ApprovedBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Device>()
            .HasOne(d => d.DeviceRequest)
            .WithOne(r => r.ApprovedDevice)
            .HasForeignKey<Device>(d => d.OriginalRequestId);

        // ── QRToken relationships ──

        modelBuilder.Entity<QRToken>()
            .HasOne(q => q.Device)
            .WithMany(d => d.QRTokens)
            .HasForeignKey(q => q.DeviceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<QRToken>()
            .HasOne(q => q.Student)
            .WithMany()
            .HasForeignKey(q => q.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<QRToken>()
            .HasOne(q => q.AdminRevoker)
            .WithMany()
            .HasForeignKey(q => q.RevokedBy)
            .OnDelete(DeleteBehavior.SetNull);

        // ── RFIDCard relationships ──

        modelBuilder.Entity<RFIDCard>()
            .HasOne(r => r.Student)
            .WithMany(s => s.RFIDCards)
            .HasForeignKey(r => r.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RFIDCard>()
            .HasOne(r => r.AdminIssuer)
            .WithMany()
            .HasForeignKey(r => r.IssuedBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RFIDCard>()
            .HasOne(r => r.AdminInvalidator)
            .WithMany()
            .HasForeignKey(r => r.InvalidatedBy)
            .OnDelete(DeleteBehavior.SetNull);

        // ── GateScanLog relationships ──

        modelBuilder.Entity<GateScanLog>()
            .HasOne(l => l.Scanner)
            .WithMany(s => s.GateScanLogs)
            .HasForeignKey(l => l.ScannerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Turn off cascading on the log table to avoid cycles
        foreach (var foreignKey in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            if (foreignKey.DeclaringEntityType.ClrType == typeof(GateScanLog))
            {
                foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
            }
        }
    }
}
