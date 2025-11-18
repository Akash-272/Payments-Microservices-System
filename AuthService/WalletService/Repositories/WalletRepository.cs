using Microsoft.EntityFrameworkCore;
using WalletService.Data;
using WalletService.Entities;

namespace WalletService.Repositories
{
    public class WalletRepository : IWalletRepository
    {
        private readonly WalletDbContext _db;
        public WalletRepository(WalletDbContext db) => _db = db;

        public async Task<Wallet?> GetByUserIdAsync(string userId)
        {
            return await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
        }

        public async Task<Wallet> CreateAsync(Wallet wallet)
        {
            await _db.Wallets.AddAsync(wallet);
            return wallet;
        }

        public Task UpdateAsync(Wallet wallet)
        {
            _db.Wallets.Update(wallet);
            return Task.CompletedTask;
        }

        public async Task AddTransactionAsync(WalletTransaction tx)
        {
            await _db.WalletTransactions.AddAsync(tx);
        }

        public async Task<IEnumerable<WalletTransaction>> GetTransactionsAsync(int walletId, int take = 50)
        {
            return await _db.WalletTransactions
                .AsNoTracking()
                .Where(t => t.WalletId == walletId)
                .OrderByDescending(t => t.CreatedAt)
                .Take(take)
                .ToListAsync();
        }

        public Task SaveChangesAsync() => _db.SaveChangesAsync();
    }
}
