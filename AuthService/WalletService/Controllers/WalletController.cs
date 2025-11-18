using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WalletService.DTOs;
using WalletService.Services;

namespace WalletService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WalletController : ControllerBase
    {
        private readonly IWalletService _service;
        public WalletController(IWalletService service) => _service = service;

        // Helper to read user id from sub claim
        private string GetUserId()
        {
            var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? User.FindFirst("sub")?.Value
                      ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

            if (string.IsNullOrEmpty(sub)) throw new InvalidOperationException("User id not found in token");
            return sub;
        }

        [HttpPost]
        public async Task<IActionResult> CreateWallet()
        {
            var userId = GetUserId();
            var dto = await _service.GetOrCreateWalletAsync(userId);
            return CreatedAtAction(nameof(GetWallet), new { userId }, dto);
        }

        [HttpGet("{userId?}")]
        public async Task<IActionResult> GetWallet(string? userId = null)
        {
            userId ??= GetUserId();
            var dto = await _service.GetOrCreateWalletAsync(userId);
            return Ok(dto);
        }

        [HttpPost("credit")]
        public async Task<IActionResult> Credit([FromBody] CreditDto req)
        {
            var userId = GetUserId();
            var dto = await _service.CreditAsync(userId, req.Amount, req.Reference);
            return Ok(dto);
        }

        [HttpPost("debit")]
        public async Task<IActionResult> Debit([FromBody] DebitDto req)
        {
            var userId = GetUserId();
            var dto = await _service.DebitAsync(userId, req.Amount, req.Reference);
            return Ok(dto);
        }

        [HttpPost("transfer")]
        public async Task<IActionResult> Transfer([FromBody] TransferDto req)
        {
            var fromUserId = GetUserId();
            var dto = await _service.TransferAsync(fromUserId, req.ToUserId, req.Amount, req.Reference);
            return Ok(dto);
        }

        [HttpGet("transactions")]
        public async Task<IActionResult> Transactions([FromQuery] int take = 50)
        {
            var userId = GetUserId();
            var txs = await _service.GetTransactionsAsync(userId, take);
            return Ok(txs);
        }
    }
}
