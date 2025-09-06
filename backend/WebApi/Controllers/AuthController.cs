using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebApi.Data;
using WebApi.Models;

namespace WebApi.Controllers;

public record LoginDto(string Email, string Password);
public record TokenResponse(string AccessToken, string Role, string UserId);

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _cfg;
    private readonly AppDbContext _db;

    public AuthController(IConfiguration cfg, AppDbContext db)
    {
        _cfg = cfg;
        _db  = db;
    }

    [HttpPost("token")]
        public async Task<ActionResult<TokenResponse>> Token([FromBody] LoginDto dto, CancellationToken ct)
        {
            if (dto is null || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest("Email e senha são obrigatórios.");

            var email = dto.Email.Trim();

            var cliente = await _db.Clientes
                .AsNoTracking()
                .Include(c => c.Usuario) 
                .FirstOrDefaultAsync(c => c.Email == email && c.Senha == dto.Password, ct);

            if (cliente is null)
                return Unauthorized(new { message = "E-mail ou senha inválidos." });

            var isAprovador = cliente.Usuario?.TipoUsuario == TipoUsuario.APROVADOR;
            var role = isAprovador ? "APROVADOR" : "CLIENTE";
            var userId = isAprovador ? $"approver-{cliente.Id:D2}" : $"client-{cliente.Id:D2}";

            var token = IssueJwt(
                userId: userId,
                email: cliente.Email,
                role: role,
                clientId: isAprovador ? null : cliente.Id.ToString()
            );

            return Ok(new TokenResponse(token, role, userId));
        }

    private string IssueJwt(string userId, string email, string role, string? clientId)
    {
        var section = _cfg.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(section["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Role, role),
            new("role", role)
        };
        if (!string.IsNullOrWhiteSpace(clientId))
            claims.Add(new Claim("clientId", clientId));

        var token = new JwtSecurityToken(
            issuer: section["Issuer"],
            audience: section["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
