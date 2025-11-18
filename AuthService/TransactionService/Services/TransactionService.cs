using TransactionService.DTOs;
using TransactionService.Entities;
using TransactionService.Repositories;

namespace TransactionService.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly ILedgerRepository _repo;

        public TransactionService(ILedgerRepository repo)
        {
            _repo = repo;
        }

        public async Task CreateLedgerEntryAsync(string userId, string walletId, string txType, decimal amount, string? reference = null)
        {
            var entry = new LedgerEntry
            {
                UserId = userId,
                WalletId = walletId,
                TransactionType = txType,
                Amount = amount,
                Reference = reference,
                Timestamp = DateTime.UtcNow
            };

            await _repo.AddAsync(entry);
            await _repo.SaveChangesAsync();
        }

        public async Task<IEnumerable<LedgerResponseDto>> GetUserTransactionsAsync(string userId, int take = 100)
        {
            var list = await _repo.GetByUserAsync(userId, take);
            return list.Select(e => new LedgerResponseDto
            {
                Id = e.Id,
                UserId = e.UserId,
                WalletId = e.WalletId,
                TransactionType = e.TransactionType,
                Amount = e.Amount,
                Reference = e.Reference,
                Timestamp = e.Timestamp
            });
        }

        public async Task<IEnumerable<LedgerResponseDto>> GetTransactionsByRangeAsync(DateTime from, DateTime to)
        {
            var list = await _repo.GetByRangeAsync(from, to);
            return list.Select(e => new LedgerResponseDto
            {
                Id = e.Id,
                UserId = e.UserId,
                WalletId = e.WalletId,
                TransactionType = e.TransactionType,
                Amount = e.Amount,
                Reference = e.Reference,
                Timestamp = e.Timestamp
            });
        }
    }
}
