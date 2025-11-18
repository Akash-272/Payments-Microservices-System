using WalletService.DTOs;

namespace WalletService.Services
{
    public interface IWalletService
    {
        Task<WalletDto> GetOrCreateWalletAsync(string userId);
        Task<WalletDto> CreditAsync(string userId, decimal amount, string? reference = null);
        Task<WalletDto> DebitAsync(string userId, decimal amount, string? reference = null);
        Task<WalletDto> TransferAsync(string fromUserId, string toUserId, decimal amount, string? reference = null);
        Task<IEnumerable<object>> GetTransactionsAsync(string userId, int take = 50);
    }
}
