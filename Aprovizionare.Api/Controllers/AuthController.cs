using Aprovizionare.Api.Dtos;
using Aprovizionare.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Aprovizionare.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager)
    : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var result = await signInManager.PasswordSignInAsync(
            dto.Email, dto.Password, isPersistent: false, lockoutOnFailure: false);

        if (!result.Succeeded)
            return Unauthorized(new { message = "Email sau parolă greșită." });

        return Ok();
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<MeDto>> Me()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized();

        var roles = await userManager.GetRolesAsync(user);
        return new MeDto(user.Email!, user.FullName, roles);
    }
}
