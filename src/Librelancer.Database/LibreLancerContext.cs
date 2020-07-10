namespace LibreLancer.Database
{
    using LibreLancer.Entities.Character;

    using Microsoft.EntityFrameworkCore;

    public class LibreLancerContext : DbContext
    {
        private string DatabaseName { get; }
        private bool UseLazyLoading { get; }
        public LibreLancerContext(string databaseName, DbContextOptions<DbContext> options, bool useLazyLoading = true) : base(options)
        {
            DatabaseName = databaseName;
            UseLazyLoading = useLazyLoading;
        }

        public DbSet<Character> Characters { get; set; }
        public DbSet<Account> Accounts { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=" + DatabaseName);

            if (UseLazyLoading)
                optionsBuilder.UseLazyLoadingProxies();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Indexes
            modelBuilder.Entity<Account>().HasIndex(x => x.AccountIdentifier);

            // Define relationships
            modelBuilder.Entity<Account>().HasMany(x => x.Characters).WithOne(x => x.Account);
        }
    }
}
