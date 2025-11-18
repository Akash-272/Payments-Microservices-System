namespace TransactionService.DTOs
{
    public class LedgerResponseDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = null!;
        public string WalletId { get; set; } = null!;
        public string TransactionType { get; set; } = null!;
        public decimal Amount { get; set; }
        public string? Reference { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
