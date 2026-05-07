using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FinTrack.API.Models;
using Microsoft.IdentityModel.Tokens;

namespace FinTrack.API.Services;

public class AuthService
{
    private readonly DatabaseService _db;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(DatabaseService db, IConfiguration configuration, ILogger<AuthService> logger)
    {
        _db = db;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        if (request.Password.Length < 4)
            throw new ArgumentException("Password must be at least 4 characters.");

        var existing = await _db.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM Users WHERE Email = @Email",
            new { request.Email });

        if (existing != null)
            throw new InvalidOperationException("An account with this email already exists.");

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        var role = string.IsNullOrEmpty(request.Role) ? "user" : request.Role;

        var sql = @"
            INSERT INTO Users (Email, FullName, PasswordHash, Role, IsAdmin, PhoneNumber, Currency, TimeZone)
            OUTPUT INSERTED.*
            VALUES (@Email, @FullName, @PasswordHash, @Role, @IsAdmin, @PhoneNumber, @Currency, @TimeZone)";

        var user = await _db.QueryFirstOrDefaultAsync<User>(sql, new
        {
            request.Email,
            request.FullName,
            PasswordHash = passwordHash,
            Role = role,
            IsAdmin = role == "admin",
            request.PhoneNumber,
            request.Currency,
            request.TimeZone
        });

        if (user == null)
            throw new Exception("Failed to create user account.");

        _logger.LogInformation("New user registered: {Email}", request.Email);

        var token = GenerateJwtToken(user);
        return BuildAuthResponse(user, token);
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _db.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM Users WHERE Email = @Email AND IsActive = 1",
            new { request.Email });

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        await _db.ExecuteAsync(
            "UPDATE Users SET LastLoginAt = @Now WHERE Id = @Id",
            new { Now = DateTime.UtcNow, user.Id });

        var token = GenerateJwtToken(user);
        _logger.LogInformation("User logged in: {Email}", request.Email);

        return BuildAuthResponse(user, token);
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _db.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM Users WHERE Id = @Id AND IsActive = 1",
            new { Id = userId });
    }

    public int GetCurrentUserId(ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst(ClaimTypes.NameIdentifier)
            ?? principal.FindFirst("sub");

        if (claim == null || !int.TryParse(claim.Value, out var userId))
            throw new UnauthorizedAccessException("Invalid token claims.");

        return userId;
    }

    private string GenerateJwtToken(User user)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var secretKey = jwtSection["Secret"]!;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiryHours = jwtSection.GetValue<int>("ExpiryHours", 24);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role),
            new("uid", user.Id.ToString()),
            new("isAdmin", user.IsAdmin.ToString().ToLower()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            issuer: jwtSection["Issuer"],
            audience: jwtSection["Audience"],
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddHours(expiryHours),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static AuthResponse BuildAuthResponse(User user, string token)
    {
        return new AuthResponse
        {
            Token = token,
            TokenType = "Bearer",
            ExpiresIn = 86400,
            User = new UserProfileResponse
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
            }
        };
    }
}
