using System.Security.Claims;
using System.Threading.Tasks;
using ODULink.Models;
using ODULink.DTOs;

namespace ODULink.Services
{
    public interface ITokenService
    {
        TokenDto GenerateTokens(Company company);
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
        Task<TokenDto> RefreshTokens(string refreshToken);
    }
}
