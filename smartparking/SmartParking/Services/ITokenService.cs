using System.Security.Claims;
using System.Threading.Tasks;
using SmartParking.Models; // <--- ADD THIS USING for Company model
using SmartParking.DTOs;  // <--- ADD THIS USING for TokenDto

namespace SmartParking.Services // <--- ADD THIS NAMESPACE
{
    public interface ITokenService
    {
        TokenDto GenerateTokens(Company company);
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
        Task<TokenDto> RefreshTokens(string refreshToken);
    }
}