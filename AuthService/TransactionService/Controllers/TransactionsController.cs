using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TransactionService.DTOs;
using TransactionService.Services;

namespace TransactionService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TransactionsController : ControllerBase
    {
        private readonly ITransactionService _service;
        public TransactionsController(ITransactionService service) => _service = service;

        private string GetUserId()
        {
            var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? User.FindFirst("sub")?.Value
                      ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
            if (string.IsNullOrEmpty(sub)) throw new InvalidOperationException("User id not found in token");
            return sub;
        }

        [HttpGet("user/{userId?}")]
        public async Task<IActionResult> GetUserTransactions(string? userId = null, [FromQuery] int take = 100)
        {
            userId ??= GetUserId();
            var txs = await _service.GetUserTransactionsAsync(userId, take);
            return Ok(txs);
        }

        [HttpGet("range")]
        public async Task<IActionResult> GetByRange([FromQuery] DateTime from, [FromQuery] DateTime to)
        {
            var txs = await _service.GetTransactionsByRangeAsync(from, to);
            return Ok(txs);
        }
    }
}
