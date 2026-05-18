using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Sit_in_Monitoring.Models;

namespace Sit_in_Monitoring.Data
{
    public class AppDbContext : IdentityDbContext<User>
    {
        public DbSet<SitIn> SitIns { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<Announcement> Announcements { get; set; }
        public DbSet<LabPC> LabPCs { get; set; }
        public DbSet<StudentNotification> StudentNotifications { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure SitIn relationships
            builder.Entity<SitIn>()
                .HasOne(s => s.User)
                .WithMany(u => u.SitIns)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Reservation relationships
            builder.Entity<Reservation>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure StudentNotification relationships
            builder.Entity<StudentNotification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure LabPC if it has UserId foreign key
            builder.Entity<LabPC>()
                .HasKey(p => p.Id);

            // Configure Announcement if it has UserId foreign key
            builder.Entity<Announcement>()
                .HasKey(a => a.Id);
        }
    }
}
