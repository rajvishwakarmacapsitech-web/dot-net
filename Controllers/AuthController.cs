using api.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using api.Services;
using System.Text.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
namespace api.Controllers;


[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IMongoCollection<User> _users;
    private readonly JwtService _jwtService;


    public AuthController(IMongoDatabase database, JwtService jwtService)
    {
        _users = database.GetCollection<User>("users");
        _jwtService = jwtService;
    }

    [HttpPost("signup")]
    public async Task<IActionResult> Signup([FromBody] RegisterRequest request)
    {
        var existingUser = await _users
            .Find(x => x.Email == request.Email)
            .FirstOrDefaultAsync();

        if (existingUser != null)
        {
            if (existingUser.Status == UserStatus.Inactive)
            {
                return BadRequest(new
                {
                    message = "User is inactive. Please contact admin."
                });
            }

            if (!existingUser.isEmailVerified)
            {
                // call OTP service
                return BadRequest(new
                {
                    message = "OTP sent to your email, please verify."
                });
            }

            return BadRequest(new
            {
                message = "User already exists with this email."
            });
        }

        // Create User from RegisterRequest
        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            Gender = request.Gender,
            Dob = request.Dob,
            Address = request.Address,

            // Server controlled fields
            Status = UserStatus.Active,
            Role = UserRole.User,
            isEmailVerified = true,

            // Hash password
            password = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        await _users.InsertOneAsync(user);

        return Ok(new
        {
            message = "User registered successfully"
        });
    }


    [HttpPost("signin")]
    public async Task<IActionResult> Signin([FromBody] LoginRequest req, [FromServices] IHttpContextAccessor httpContextAccessor)
    {
        var existingUser = await _users.Find(x => x.Email == req.Email).FirstOrDefaultAsync();

        if (existingUser == null)
        {
            return BadRequest(new { message = "User not found." });
        }
        if(!existingUser.isEmailVerified)
        {
            // we can call otp service here ->
            return BadRequest(new { message = "Please verify your email first." });
        }

        if(existingUser.Status == UserStatus.Inactive)
        {
            return BadRequest(new { message = "User is inactive. Please contact admin." });
        }

        var passwordValid = BCrypt.Net.BCrypt.Verify(
            req.Password,
            existingUser.password
         );

        if (!passwordValid)
        {
            return BadRequest(new
            {
                message = "Invalid email or password."
            });
        }

        var accessToken =
        _jwtService.GenerateAccessToken(existingUser);

        var refreshToken =
            _jwtService.GenerateRefreshToken(existingUser);

        httpContextAccessor.HttpContext?.Response.Cookies.Append(
            "accessToken",
            accessToken);

        httpContextAccessor.HttpContext?.Response.Cookies.Append(
            "refreshToken",
            refreshToken);

        return Ok(new
        {
            accessToken,
            user = new
            {
                existingUser.Id,
                existingUser.Name,
                existingUser.Email,
                existingUser.Phone,
                existingUser.Gender,
                existingUser.Dob,
                existingUser.Address,
                existingUser.Status,
                existingUser.isEmailVerified,
                existingUser.Role
            }
        });
    }
    [Authorize]
    [HttpPost("profile")]
    public async Task<IActionResult> Profile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var user = await _users.Find(x => x.Id == userId).FirstOrDefaultAsync();
        if(user == null)
        {
            return NotFound(new { message = "User not found." });
        }
        return Ok(new
        {
            user.Id,
            user.Name,
            user.Email,
            user.Phone,
            user.Gender,
            user.Dob,
            user.Address,
            user.Status,
            user.Role
        });

    }
}