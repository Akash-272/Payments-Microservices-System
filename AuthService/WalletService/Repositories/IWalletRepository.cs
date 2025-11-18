using WalletService.Entities;

namespace WalletService.Repositories
{
    public interface IWalletRepository
    {
        Task<Wallet?> GetByUserIdAsync(string userId);
        Task<Wallet> CreateAsync(Wallet wallet);
        Task UpdateAsync(Wallet wallet);
        Task AddTransactionAsync(WalletTransaction tx);
        Task<IEnumerable<WalletTransaction>> GetTransactionsAsync(int walletId, int take = 50);
        Task SaveChangesAsync();
    }
}
