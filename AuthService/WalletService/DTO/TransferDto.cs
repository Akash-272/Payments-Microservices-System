using System.ComponentModel.DataAnnotations;

namespace WalletService.DTOs
{
    public class TransferDto
    {
        [Required] public string ToUserId { get; set; } = null!;
        [Required] public decimal Amount { get; set; }
        public string? Reference { get; set; }
    }
}
