using TransactionService.Entities;

namespace TransactionService.Repositories
{
    public interface ILedgerRepository
    {
        Task AddAsync(LedgerEntry entry);
        Task<IEnumerable<LedgerEntry>> GetByUserAsync(string userId, int take = 100);
        Task<IEnumerable<LedgerEntry>> GetByRangeAsync(DateTime from, DateTime to);
        Task SaveChangesAsync();
    }
}
