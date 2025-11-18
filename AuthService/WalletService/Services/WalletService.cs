using Microsoft.EntityFrameworkCore;
using WalletService.Data;
using WalletService.Entities;
using WalletService.Repositories;

namespace WalletService.Services
{
    public class WalletService : IWalletService
    {
        private readonly IWalletRepository _repo;
        private readonly WalletDbContext _db; // Adjusted to use the correct namespace
        private readonly Messaging.IRabbitMqProducer _producer;

        public WalletService(IWalletRepository repo, WalletDbContext db, Messaging.IRabbitMqProducer producer)
        {
            _repo = repo;
            _db = db;
            _producer = producer;
        }

        public async Task<DTOs.WalletDto> GetOrCreateWalletAsync(string userId)
        {
            var wallet = await _repo.GetByUserIdAsync(userId);
            if (wallet != null)
            {
                return new DTOs.WalletDto { WalletId = wallet.Id, UserId = wallet.UserId, Balance = wallet.Balance };
            }

            var newWallet = new Wallet { UserId = userId, Balance = 0m };
            await _repo.CreateAsync(newWallet);
            await _repo.SaveChangesAsync();

            return new DTOs.WalletDto { WalletId = newWallet.Id, UserId = newWallet.UserId, Balance = newWallet.Balance };
        }

        public async Task<DTOs.WalletDto> CreditAsync(string userId, decimal amount, string? reference = null)
        {
            if (amount <= 0) throw new ArgumentException("Amount must be positive", nameof(amount));

            using var tx = await _db.Database.BeginTransactionAsync();
            var wallet = await _repo.GetByUserIdAsync(userId) ?? new Wallet { UserId = userId, Balance = 0m };

            if (wallet == null) // newly created in memory
            {
                await _repo.CreateAsync(wallet);
            }

            wallet.Balance += amount;
            wallet.UpdatedAt = DateTime.UtcNow;
            await _repo.UpdateAsync(wallet);

            var trx = new WalletTransaction
            {
                WalletId = wallet.Id,
                Type = "CREDIT",
                Amount = amount,
                Reference = reference
            };
            await _repo.AddTransactionAsync(trx);

            await _repo.SaveChangesAsync();
            await tx.CommitAsync();

            // publish event (simple JSON)
            var evt = System.Text.Json.JsonSerializer.Serialize(new
            {
                Event = "WalletCredited",
                WalletId = wallet.Id,
                UserId = wallet.UserId,
                Amount = amount,
                Reference = reference,
                CreatedAt = DateTime.UtcNow
            });
            await _producer.PublishAsync("wallet.credited", evt);

            return new DTOs.WalletDto { WalletId = wallet.Id, UserId = wallet.UserId, Balance = wallet.Balance };
        }

        public async Task<DTOs.WalletDto> DebitAsync(string userId, decimal amount, string? reference = null)
        {
            if (amount <= 0) throw new ArgumentException("Amount must be positive", nameof(amount));

            using var tx = await _db.Database.BeginTransactionAsync();
            var wallet = await _repo.GetByUserIdAsync(userId);
            if (wallet == null) throw new InvalidOperationException("Wallet not found");

            if (wallet.Balance < amount) throw new InvalidOperationException("Insufficient funds");

            wallet.Balance -= amount;
            wallet.UpdatedAt = DateTime.UtcNow;
            await _repo.UpdateAsync(wallet);

            var trx = new WalletTransaction
            {
                WalletId = wallet.Id,
                Type = "DEBIT",
                Amount = amount,
                Reference = reference
            };
            await _repo.AddTransactionAsync(trx);

            await _repo.SaveChangesAsync();
            await tx.CommitAsync();

            var evt = System.Text.Json.JsonSerializer.Serialize(new
            {
                Event = "WalletDebited",
                WalletId = wallet.Id,
                UserId = wallet.UserId,
                Amount = amount,
                Reference = reference,
                CreatedAt = DateTime.UtcNow
            });
            await _producer.PublishAsync("wallet.debited", evt);

            return new DTOs.WalletDto { WalletId = wallet.Id, UserId = wallet.UserId, Balance = wallet.Balance };
        }

        public async Task<DTOs.WalletDto> TransferAsync(string fromUserId, string toUserId, decimal amount, string? reference = null)
        {
            if (fromUserId == toUserId) throw new ArgumentException("Cannot transfer to same user");
            if (amount <= 0) throw new ArgumentException("Amount must be positive");

            using var tx = await _db.Database.BeginTransactionAsync();

            var fromWallet = await _repo.GetByUserIdAsync(fromUserId) ?? throw new InvalidOperationException("Sender wallet not found");
            if (fromWallet.Balance < amount) throw new InvalidOperationException("Insufficient funds");

            var toWallet = await _repo.GetByUserIdAsync(toUserId);
            if (toWallet == null)
            {
                toWallet = new Wallet { UserId = toUserId, Balance = 0m };
                await _repo.CreateAsync(toWallet);
            }

            fromWallet.Balance -= amount;
            fromWallet.UpdatedAt = DateTime.UtcNow;
            await _repo.UpdateAsync(fromWallet);

            toWallet.Balance += amount;
            toWallet.UpdatedAt = DateTime.UtcNow;
            await _repo.UpdateAsync(toWallet);

            var txOut = new WalletTransaction
            {
                WalletId = fromWallet.Id,
                Type = "TRANSFER_OUT",
                Amount = amount,
                RelatedUserId = toUserId,
                Reference = reference
            };
            await _repo.AddTransactionAsync(txOut);

            var txIn = new WalletTransaction
            {
                WalletId = toWallet.Id,
                Type = "TRANSFER_IN",
                Amount = amount,
                RelatedUserId = fromUserId,
                Reference = reference
            };
            await _repo.AddTransactionAsync(txIn);

            await _repo.SaveChangesAsync();
            await tx.CommitAsync();

            var evt = System.Text.Json.JsonSerializer.Serialize(new
            {
                Event = "WalletTransferred",
                FromUserId = fromUserId,
                ToUserId = toUserId,
                Amount = amount,
                Reference = reference,
                CreatedAt = DateTime.UtcNow
            });
            await _producer.PublishAsync("wallet.transferred", evt);

            return new DTOs.WalletDto { WalletId = fromWallet.Id, UserId = fromWallet.UserId, Balance = fromWallet.Balance };
        }

        public async Task<IEnumerable<object>> GetTransactionsAsync(string userId, int take = 50)
        {
            var wallet = await _repo.GetByUserIdAsync(userId) ?? throw new InvalidOperationException("Wallet not found");
            var txs = await _repo.GetTransactionsAsync(wallet.Id, take);
            return txs.Select(t => new
            {
                t.Id,
                t.Type,
                t.Amount,
                t.RelatedUserId,
                t.Reference,
                t.CreatedAt
            });
        }
    }
}
