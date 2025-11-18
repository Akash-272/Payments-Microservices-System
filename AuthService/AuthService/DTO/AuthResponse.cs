namespace AuthService.API.DTOs
{
    public class AuthResponse
    {
        public string Token { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
        public int UserId { get; set; }
    }
}
