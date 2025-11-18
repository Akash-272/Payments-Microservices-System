using System.ComponentModel.DataAnnotations;

namespace WalletService.Entities
{
    public class WalletTransaction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int WalletId { get; set; }

        // CREDIT / DEBIT / TRANSFER
        [Required]
        public string Type { get; set; } = null!;

        [Required]
        public decimal Amount { get; set; }

        // optional: for transfer - target user id
        public string? RelatedUserId { get; set; }

        public string? Reference { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
