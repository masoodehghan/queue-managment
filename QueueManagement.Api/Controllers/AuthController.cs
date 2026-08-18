using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QueueManagement.Application.Common.Interfaces;
using QueueManagement.Application.DTOs.Auth;
using QueueManagement.Domain.Entities.Users;

namespace QueueManagement.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    UserManager<ApplicationUser> userManager,
    IJwtTokenService jwtTokenService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(
        RegisterDto dto)
    {
        var user = new ApplicationUser
        {
            Email = dto.Email.Trim(),
            UserName = dto.Email.Trim(),
            FullName = dto.FullName.Trim()
        };

        var result = await userManager.CreateAsync(user, dto.Password);

        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            return ValidationProblem(ModelState);
        }

        return Ok(CreateAuthResponse(user));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(
        LoginDto dto)
    {
        var user = await userManager.FindByEmailAsync(dto.Email.Trim());

        if (user is null ||
            !await userManager.CheckPasswordAsync(user, dto.Password))
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Invalid credentials",
                detail: "The email address or password is incorrect.");
        }

        return Ok(CreateAuthResponse(user));
    }

    [HttpPost("token")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> Token(
        [FromForm(Name = "username")] string username,
        [FromForm(Name = "password")] string password)
    {
        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password))
        {
            return Unauthorized(new
            {
                error = "invalid_grant",
                error_description = "Username and password are required."
            });
        }

        var user = await userManager.FindByEmailAsync(username.Trim());

        if (user is null ||
            !await userManager.CheckPasswordAsync(user, password))
        {
            return Unauthorized(new
            {
                error = "invalid_grant",
                error_description = "The username or password is incorrect."
            });
        }

        return Ok(new
        {
            access_token = jwtTokenService.CreateToken(user),
            token_type = "Bearer",
            expires_in = jwtTokenService.ExpirationSeconds
        });
    }

    private AuthResponseDto CreateAuthResponse(ApplicationUser user) =>
        new(
            jwtTokenService.CreateToken(user),
            user.Id,
            user.Email ?? string.Empty,
            user.FullName);

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(error.Code, error.Description);
        }
    }
}
