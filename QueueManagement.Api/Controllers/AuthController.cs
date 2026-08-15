using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using QueueManagement.Application.DTOs.Auth;
using QueueManagement.Domain.Entities.Users;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace QueueManagement.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController(UserManager<ApplicationUser> userManager, IConfiguration configuration)
    : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FullName = dto.FullName
        };

        var result = await userManager.CreateAsync(user, dto.Password);

        if (!result.Succeeded)
            return BadRequest(result.Errors);

        var token = await GenerateToken(user);

        return Ok(new AuthResponseDto(token, user.Id, user.Email!, user.FullName));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var user = await userManager.FindByEmailAsync(dto.Email);
        
        if (user == null || !await userManager.CheckPasswordAsync(user, dto.Password))
            return Unauthorized("Invalid credentials");

        var token = await GenerateToken(user);

        return Ok(new AuthResponseDto(token, user.Id, user.Email!, user.FullName));
    }
    
    [HttpPost("token")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> Token()
    {
        var form = await Request.ReadFormAsync();

        var username = form["username"].ToString();
        var password = form["password"].ToString();

        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password))
        {
            return Unauthorized(new
            {
                error = "invalid_grant"
            });
        }

        var user = await userManager.FindByEmailAsync(username);

        if (user == null)
        {
            return Unauthorized(new
            {
                error = "invalid_grant"
            });
        }
        
        if (!await userManager.CheckPasswordAsync(
                user,
                password))
        {
            return Unauthorized(new
            {
                error = "invalid_grant"
            });
        }

        var accessToken = await GenerateToken(user);

        return Ok(new
        {
            access_token = accessToken,
            token_type = "Bearer",
            expires_in =  8 * 60
        });
    }

    private async Task<string> GenerateToken(ApplicationUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Name, user.FullName)
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
        
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}