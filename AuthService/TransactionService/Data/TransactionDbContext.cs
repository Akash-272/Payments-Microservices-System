using Microsoft.EntityFrameworkCore;
using TransactionService.Entities;

namespace TransactionService.Data
{
    public class TransactionDbContext : DbContext
    {
        public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();

        public TransactionDbContext(DbContextOptions<TransactionDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<LedgerEntry>()
                .HasIndex(e => e.UserId);

            modelBuilder.Entity<LedgerEntry>()
                .HasIndex(e => e.WalletId);

            base.OnModelCreating(modelBuilder);
        }
    }
}
