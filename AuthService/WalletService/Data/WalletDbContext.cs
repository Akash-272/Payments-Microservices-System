using Microsoft.EntityFrameworkCore;
using WalletService.Entities;

namespace WalletService.Data
{
    public class WalletDbContext : DbContext
    {
        public DbSet<Wallet> Wallets => Set<Wallet>();
        public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();

        public WalletDbContext(DbContextOptions<WalletDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Wallet>()
                .HasIndex(w => w.UserId);

            modelBuilder.Entity<WalletTransaction>()
                .HasIndex(t => t.WalletId);

            base.OnModelCreating(modelBuilder);
        }
    }
}
