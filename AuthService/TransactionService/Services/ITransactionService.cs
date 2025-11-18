using TransactionService.DTOs;

namespace TransactionService.Services
{
    public interface ITransactionService
    {
        Task CreateLedgerEntryAsync(string userId, string walletId, string txType, decimal amount, string? reference = null);
        Task<IEnumerable<LedgerResponseDto>> GetUserTransactionsAsync(string userId, int take = 100);
        Task<IEnumerable<LedgerResponseDto>> GetTransactionsByRangeAsync(DateTime from, DateTime to);
    }
}
