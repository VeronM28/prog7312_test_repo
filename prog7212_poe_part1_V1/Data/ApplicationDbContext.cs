// Data/ApplicationDbContext.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using prog7212_poe_part1_V1.Models;

namespace prog7212_poe_part1_V1.Data
{
    public class ApplicationDbContext : IdentityDbContext<UserModel>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<ReportModel> Reports { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<UserPreference> UserPreferences { get; set; }
        public DbSet<ServiceRequestModel> ServiceRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure Report-User relationship
            builder.Entity<ReportModel>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Event
            builder.Entity<Event>()
                .Property(e => e.Price)
                .HasPrecision(18, 2);

            // Configure ServiceRequestModel - SIMPLIFIED VERSION
            builder.Entity<ServiceRequestModel>(entity =>
            {
                // Primary Key
                entity.HasKey(e => e.RequestId);

                // Properties
                entity.Property(e => e.RequestId)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.UserId)
                    .IsRequired(false) // Make it optional in database
                    .HasMaxLength(450);

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(1000);

                entity.Property(e => e.Address)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.AssignedAdmin)
                    .HasMaxLength(450);

                entity.Property(e => e.AdminNotes)
                    .HasMaxLength(2000);

                // Enum conversions
                entity.Property(e => e.Status)
                    .HasConversion<string>()
                    .HasMaxLength(20);

                entity.Property(e => e.ServiceType)
                    .HasConversion<string>()
                    .HasMaxLength(50);

                // Indexes
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.CreatedDate);
                entity.HasIndex(e => e.ServiceType);
            });
        }
    }
}