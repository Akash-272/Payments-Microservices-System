using Microsoft.EntityFrameworkCore;
using TransactionService.Data;
using TransactionService.Entities;

namespace TransactionService.Repositories
{
    public class LedgerRepository : ILedgerRepository
    {
        private readonly TransactionDbContext _db;
        public LedgerRepository(TransactionDbContext db) => _db = db;

        public async Task AddAsync(LedgerEntry entry)
        {
            await _db.LedgerEntries.AddAsync(entry);
        }

        public async Task<IEnumerable<LedgerEntry>> GetByUserAsync(string userId, int take = 100)
        {
            return await _db.LedgerEntries
                .AsNoTracking()
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.Timestamp)
                .Take(take)
                .ToListAsync();
        }

        public async Task<IEnumerable<LedgerEntry>> GetByRangeAsync(DateTime from, DateTime to)
        {
            return await _db.LedgerEntries
                .AsNoTracking()
                .Where(e => e.Timestamp >= from && e.Timestamp <= to)
                .OrderByDescending(e => e.Timestamp)
                .ToListAsync();
        }

        public Task SaveChangesAsync() => _db.SaveChangesAsync();
    }
}
