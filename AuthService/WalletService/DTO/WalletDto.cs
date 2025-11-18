namespace WalletService.DTOs
{
    public class WalletDto
    {
        public int WalletId { get; set; }
        public string UserId { get; set; } = null!;
        public decimal Balance { get; set; }
    }
}
