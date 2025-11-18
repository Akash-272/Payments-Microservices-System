using System.ComponentModel.DataAnnotations;

namespace TransactionService.Entities
{
    public class LedgerEntry
    {
        [Key]
        public int Id { get; set; }

        // store as string like other services (supports GUID or numeric)
        [Required]
        public string UserId { get; set; } = null!;

        [Required]
        public string WalletId { get; set; } = null!;

        [Required]
        public string TransactionType { get; set; } = null!; // CREDIT/DEBIT/TRANSFER

        [Required]
        public decimal Amount { get; set; }

        public string? Reference { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
