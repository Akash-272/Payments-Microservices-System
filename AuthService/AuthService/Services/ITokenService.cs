using AuthService.API.Models;

namespace AuthService.Services
{
    public interface ITokenService
    {
        string CreateToken(User user);
    }
}
