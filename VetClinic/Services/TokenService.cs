using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using VetClinic.Models.Dtos;
using VetClinic.Models.Entities;

namespace VetClinic.Services
{
    public class TokenService : ITokenService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;

        public TokenService(UserManager<ApplicationUser> userManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
        }

        public async Task<TokenResponseDto?> CreateTokenAsync(string email, string password)
        {
            var user = await _userManager.FindByEmailAsync(email.Trim());
            if (user == null || !await _userManager.CheckPasswordAsync(user, password))
            {
                return null;
            }

            // Token'ın içine (payload) yazılan bilgiler. Cookie ile girişteki claim'lerle aynı türler kullanılır;
            // böylece controller'lar ve MCP araçları kullanıcıyı her iki yolda da ClaimTypes.NameIdentifier'dan okur
            // ve [Authorize(Roles = ...)] JWT ile de çalışır.
            var roles = await _userManager.GetRolesAsync(user);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.Email ?? string.Empty)
            };
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            // Anahtar Program.cs'te user-secrets'tan okunur (geliştirmede yoksa rastgele üretilir).
            // Aynı anahtar, gelen token'ların imzasını doğrulamak için de kullanılır.
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var expiresAt = DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("Jwt:ExpiresMinutes"));

            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                Expires = expiresAt,
                // HMAC-SHA256: token, anahtarı bilen sunucu tarafından imzalanır; içeriği değiştirilirse imza tutmaz.
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            };

            return new TokenResponseDto
            {
                Token = new JsonWebTokenHandler().CreateToken(descriptor),
                ExpiresAt = expiresAt,
                Email = user.Email ?? string.Empty,
                Roles = roles.ToList()
            };
        }
    }
}
