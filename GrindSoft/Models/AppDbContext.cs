using Microsoft.EntityFrameworkCore;

namespace GrindSoft.Models
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<Session> Sessions { get; set; }
        public DbSet<Message> Messages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Session>()
                .HasMany(cs => cs.Messages)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade); 
        }
    }
}
