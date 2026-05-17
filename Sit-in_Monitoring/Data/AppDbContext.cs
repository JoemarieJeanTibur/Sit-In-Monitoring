using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Sit_in_Monitoring.Models;

namespace Sit_in_Monitoring.Data
{
    public class AppDbContext : IdentityDbContext<User>
    {
        public DbSet<SitIn> SitIns { get; set; }

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
        }
    }
}

