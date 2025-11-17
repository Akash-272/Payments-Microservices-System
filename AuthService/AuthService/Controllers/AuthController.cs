using AuthService.API.DTOs;
using AuthService.API.Models;
using AuthService.Repositories;
using AuthService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserRepository _repo;
        private readonly ITokenService _tokenService;
        private readonly PasswordHasher<User> _passwordHasher;

        public AuthController(IUserRepository repo, ITokenService tokenService)
        {
            _repo = repo;
            _tokenService = tokenService;
            _passwordHasher = new PasswordHasher<User>();
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest req)
        {
            var existing = await _repo.GetByEmailAsync(req.Email);
            if (existing != null)
                return Conflict(new { message = "Email already registered" });

            var user = new User
            {
                Email = req.Email.ToLowerInvariant(),
                FullName = req.FullName
            };
            user.PasswordHash = _passwordHasher.HashPassword(user, req.Password);

            await _repo.AddAsync(user);
            await _repo.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMe), new { id = user.Id }, new { user.Id, user.Email, user.FullName });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            var user = await _repo.GetByEmailAsync(req.Email.ToLowerInvariant());
            if (user == null) return Unauthorized(new { message = "Invalid credentials" });

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, req.Password);
            if (result == PasswordVerificationResult.Failed) return Unauthorized(new { message = "Invalid credentials" });

            var token = _tokenService.CreateToken(user);
            var jwtSection = HttpContext.RequestServices.GetRequiredService<IConfiguration>().GetSection("JwtSettings");
            var expiryMinutes = jwtSection.GetValue<int>("ExpiryMinutes");

            return Ok(new AuthResponse
            {
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes),
                UserId = user.Id
            });
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            var userIdClaim = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            if (!Guid.TryParse(userIdClaim, out var userId)) return Unauthorized();

            var user = await _repo.GetByIdAsync(userId);
            if (user == null) return NotFound();

            return Ok(new { user.Id, user.Email, user.FullName });
        }
    }
}
