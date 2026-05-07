using FinTrack.API.Models;
using FinTrack.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly DatabaseService _db;

    public UsersController(AuthService authService, DatabaseService db)
    {
        _authService = authService;
        _db = db;
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProfile()
    {
        var userId = _authService.GetCurrentUserId(User);
        var user = await _authService.GetUserByIdAsync(userId);

        if (user == null)
            return NotFound(new { message = "User not found." });

        return Ok(new UserProfileResponse
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role,
            PhoneNumber = user.PhoneNumber,
            ProfilePictureUrl = user.ProfilePictureUrl,
            Currency = user.Currency,
            TimeZone = user.TimeZone,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        });
    }

    [HttpPut("profile")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = _authService.GetCurrentUserId(User);
        var user = await _authService.GetUserByIdAsync(userId);

        if (user == null)
            return NotFound(new { message = "User not found." });

        if (request.FullName != null) user.FullName = request.FullName;
        if (request.PhoneNumber != null) user.PhoneNumber = request.PhoneNumber;
        if (request.ProfilePictureUrl != null) user.ProfilePictureUrl = request.ProfilePictureUrl;
        if (request.Currency != null) user.Currency = request.Currency;
        if (request.TimeZone != null) user.TimeZone = request.TimeZone;
        if (request.Role != null) user.Role = request.Role;
        if (request.IsAdmin.HasValue) user.IsAdmin = request.IsAdmin.Value;
        if (request.IsActive.HasValue) user.IsActive = request.IsActive.Value;

        await _db.ExecuteAsync(@"
            UPDATE Users
            SET FullName = @FullName,
                PhoneNumber = @PhoneNumber,
                ProfilePictureUrl = @ProfilePictureUrl,
                Currency = @Currency,
                TimeZone = @TimeZone,
                Role = @Role,
                IsAdmin = @IsAdmin,
                IsActive = @IsActive
            WHERE Id = @Id",
            new
            {
                user.Id,
                user.FullName,
                user.PhoneNumber,
                user.ProfilePictureUrl,
                user.Currency,
                user.TimeZone,
                user.Role,
                user.IsAdmin,
                user.IsActive
            });

        return Ok(new UserProfileResponse
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role,
            PhoneNumber = user.PhoneNumber,
            ProfilePictureUrl = user.ProfilePictureUrl,
            Currency = user.Currency,
            TimeZone = user.TimeZone,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        });
    }

    [HttpPut("change-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest(new { message = "Current and new passwords are required." });

        if (request.NewPassword.Length < 4)
            return BadRequest(new { message = "New password must be at least 4 characters." });

        var userId = _authService.GetCurrentUserId(User);
        var user = await _authService.GetUserByIdAsync(userId);

        if (user == null)
            return NotFound(new { message = "User not found." });

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            return BadRequest(new { message = "Current password is incorrect." });

        var newHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _db.ExecuteAsync(
            "UPDATE Users SET PasswordHash = @Hash WHERE Id = @Id",
            new { Hash = newHash, Id = userId });

        return Ok(new { message = "Password changed successfully." });
    }

    [HttpDelete("account")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteAccount()
    {
        var userId = _authService.GetCurrentUserId(User);
        await _db.ExecuteAsync(
            "UPDATE Users SET IsActive = 0 WHERE Id = @Id",
            new { Id = userId });

        return Ok(new { message = "Account deactivated successfully." });
    }
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
