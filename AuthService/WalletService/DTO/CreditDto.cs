using System.ComponentModel.DataAnnotations;

namespace WalletService.DTOs
{
    public class CreditDto
    {
        [Required] public decimal Amount { get; set; }
        public string? Reference { get; set; }
    }
}
