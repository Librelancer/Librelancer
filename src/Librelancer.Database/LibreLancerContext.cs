using LibreLancer.Entities.Character;

namespace Librelancer.Database
{
    using Microsoft.EntityFrameworkCore;

    public class LibreLancerContext : DbContext
    {
        public LibreLancerContext(DbContextOptions<DbContext> options) : base(options)
        {
        }

        public DbSet<Character> Characters { get; set; }
        public DbSet<Account> Accounts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Indexes
            modelBuilder.Entity<Account>().HasIndex(x => x.AccountIdentifier);

            // Define relationships
            modelBuilder.Entity<Account>().HasMany(x => x.Characters).WithOne(x => x.Account);
        }
    }
}
