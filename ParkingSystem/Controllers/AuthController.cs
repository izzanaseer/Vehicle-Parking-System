using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkingSystem.Data;
using ParkingSystem.Dtos;
using ParkingSystem.Models;
using ParkingSystem.Services;
namespace ParkingSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    ParkingDbContext db,
    TokenService tokenService,
    IPasswordHasher<AppUser> hasher) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var email = request.Email?.Trim().ToLowerInvariant();
        
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest("Email is required.");
        }

        var exists = await db.AppUsers.AnyAsync(u => u.Email == email);
        if (exists) return Conflict("Email already registered.");

        var user = new AppUser { Email = email };
        user.PasswordHash = hasher.HashPassword(user, request.Password);

        db.AppUsers.Add(user);
        await db.SaveChangesAsync();

        return Ok("User registered.");
    }


    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var email = request.Email?.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest("Email is required.");
        }

        var user = await db.AppUsers.FirstOrDefaultAsync(u => u.Email == email);
        if (user is null) 
        {
            return Unauthorized("Invalid email or password.");
        }

        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            return Unauthorized("Invalid email or password.");
        }

        var token = tokenService.CreateToken(user);
        return Ok(new { token });
    }
    
}