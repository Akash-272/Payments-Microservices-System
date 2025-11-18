using System.ComponentModel.DataAnnotations;

namespace WalletService.Entities
{
    public class Wallet
    {
        [Key]
        public int Id { get; set; }

        // store as string so it supports GUIDs or numeric ids from Auth service
        [Required]
        public string UserId { get; set; } = null!;

        public decimal Balance { get; set; } = 0m;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
