using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using InventoryManagementSystem.DAL.Entities;
using InventoryManagementSystem.Settings;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace InventoryManagementSystem ;

public class JwtTokenService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtSettings _settings;

    public JwtTokenService(
        UserManager<ApplicationUser> userManager,
        IOptions<JwtSettings> options)
    {
        _userManager = userManager;
        _settings = options.Value;
    }

    public async Task<string> CreateTokenAsync(
        ApplicationUser user)
    {
        var roles =
            await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(
                JwtRegisteredClaimNames.Sub,
                user.Id),

            new(
                JwtRegisteredClaimNames.Email,
                user.Email ?? string.Empty),

            new(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString()),

            new(
                ClaimTypes.NameIdentifier,
                user.Id),

            new(
                ClaimTypes.Name,
                user.UserName ?? string.Empty),

            new(
                ClaimTypes.Email,
                user.Email ?? string.Empty),

            new(
                "FullName",
                user.FullName)
        };

        foreach (var role in roles)
        {
            claims.Add(
                new Claim(
                    ClaimTypes.Role,
                    role));
        }

        var key =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    _settings.Key));

        var credentials =
            new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

        var expires =
            DateTime.UtcNow.AddMinutes(
                _settings.ExpireMinutes);

        var token =
            new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                expires: expires,
                signingCredentials: credentials);

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }
}