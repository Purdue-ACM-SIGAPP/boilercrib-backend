using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MongoDB.Driver;
using SimpleWebAppReact.Entities;
using SimpleWebAppReact.Services;

namespace SimpleWebAppReact.Controllers;

/// <summary>
/// Defines endpoints for operations relating the User table
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private const string LoginFailed = "That username and password don't match an account.";

    private readonly IMongoCollection<User> _users;
    private readonly PasswordHasherService _passwordHasher;
    private readonly JwtService _jwtService;

    public UserController(MongoDbService mongoDbService, PasswordHasherService passwordHasher, JwtService jwtService)
    {
        _users = mongoDbService.Database.GetCollection<User>("user");
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
    }

    /// <summary>
    /// gets users matching the optional filters; an undefined account type is ignored
    /// </summary>
    [Authorize(Roles = nameof(UserType.Admin))]
    [HttpGet]
    public async Task<IEnumerable<UserOut>> Get(
        [FromQuery] string? username = null,
        [FromQuery] string? name = null,
        [FromQuery] string? phoneNumber = null,
        [FromQuery] int? accountType = null)
    {
        var f = Builders<User>.Filter;
        var filter = f.Empty;

        if (!string.IsNullOrEmpty(username)) filter &= f.Eq(u => u.Username, username);
        if (!string.IsNullOrEmpty(name)) filter &= f.Eq(u => u.Name, name);
        if (!string.IsNullOrEmpty(phoneNumber)) filter &= f.Eq(u => u.PhoneNumber, phoneNumber);
        if (IsValidAccountType(accountType)) filter &= f.Eq(u => u.AccountType, accountType);

        var users = await _users.Find(filter).ToListAsync();
        return users.Select(UserOut.From);
    }

    /// <summary>
    /// gets specific user with id
    /// </summary>
    [Authorize]
    [HttpGet("{id}")]
    public async Task<ActionResult<UserOut>> GetById(string id)
    {
        if (!User.IsOwnerOrAdmin(id))
        {
            return Forbid();
        }

        var user = MongoDbService.IsValidId(id) ? await _users.Find(u => u.Id == id).FirstOrDefaultAsync() : null;
        return user is null ? NotFound() : Ok(UserOut.From(user));
    }

    /// <summary>
    /// adds user entry to table
    /// </summary>
    [EnableRateLimiting("auth")]
    [HttpPost]
    public async Task<ActionResult<AuthResponse>> Post(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("A username and password are required.");
        }

        var user = new User
        {
            Username = request.Username,
            PasswordHashed = _passwordHasher.HashPassword(request.Password),
            Name = request.Name,
            PhoneNumber = request.PhoneNumber,
            AccountType = (int)UserType.Student
        };

        try
        {
            await _users.InsertOneAsync(user);
        }
        catch (MongoWriteException e) when (e.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return Conflict("That username is already taken.");
        }

        return CreatedAtAction(nameof(GetById), new { id = user.Id },
            new AuthResponse(_jwtService.CreateToken(user), UserOut.From(user)));
    }

    /// <summary>
    /// signs in: returns the account whose username and password match
    /// </summary>
    [EnableRateLimiting("auth")]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Enter your username and password.");
        }

        var user = await _users.Find(u => u.Username == request.Username).FirstOrDefaultAsync();
        if (user is null || !IsPasswordCorrect(user, request.Password))
        {
            return Unauthorized(LoginFailed);
        }

        if (user.PasswordHashed is null)
        {
            return StatusCode(StatusCodes.Status403Forbidden, "Your password must be reset before you can log in.");
        }

        return Ok(new AuthResponse(_jwtService.CreateToken(user), UserOut.From(user)));
    }

    [EnableRateLimiting("auth")]
    [HttpPost("reset-password")]
    public async Task<ActionResult> ResetPassword(ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password)
            || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return BadRequest("Enter your username, current password and new password.");
        }

        if (request.NewPassword == request.Password)
        {
            return BadRequest("Choose a new password that is different from the current one.");
        }

        var user = await _users.Find(u => u.Username == request.Username).FirstOrDefaultAsync();
        if (user is null || !IsPasswordCorrect(user, request.Password))
        {
            return Unauthorized(LoginFailed);
        }

        var update = Builders<User>.Update
            .Set(u => u.PasswordHashed, _passwordHasher.HashPassword(request.NewPassword))
            .Unset(u => u.Password);
        await _users.UpdateOneAsync(u => u.Id == user.Id, update);
        return Ok();
    }

    /// <summary>
    /// updates a user entry
    /// </summary>
    [Authorize]
    [HttpPut]
    public async Task<ActionResult> Update(UpdateUserRequest request)
    {
        if (request.AccountType.HasValue && !IsValidAccountType(request.AccountType))
        {
            return BadRequest("Invalid account type.");
        }

        if (!User.IsOwnerOrAdmin(request.Id))
        {
            return Forbid();
        }

        if (!MongoDbService.IsValidId(request.Id))
        {
            return NotFound();
        }

        var update = Builders<User>.Update.Set(u => u.Name, request.Name).Set(u => u.PhoneNumber, request.PhoneNumber);
        if (request.AccountType.HasValue && User.IsInRole(nameof(UserType.Admin)))
        {
            update = update.Set(u => u.AccountType, request.AccountType);
        }

        var result = await _users.UpdateOneAsync(u => u.Id == request.Id, update);
        return result.MatchedCount > 0 ? Ok() : NotFound();
    }

    /// <summary>
    /// deletes a user entry
    /// </summary>
    [Authorize]
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id)
    {
        if (!User.IsOwnerOrAdmin(id))
        {
            return Forbid();
        }

        if (!MongoDbService.IsValidId(id))
        {
            return NotFound();
        }

        var result = await _users.DeleteOneAsync(u => u.Id == id);
        return result.DeletedCount > 0 ? Ok() : NotFound();
    }

    private bool IsPasswordCorrect(User user, string password) =>
        user.PasswordHashed is null ? user.Password == password : _passwordHasher.VerifyPassword(password, user.PasswordHashed);

    private static bool IsValidAccountType(int? accountType) =>
        accountType.HasValue && Enum.IsDefined(typeof(UserType), accountType.Value);

    public record LoginRequest(string? Username, string? Password);

    public record RegisterRequest(string? Username, string? Password, string? Name, string? PhoneNumber);

    public record ResetPasswordRequest(string? Username, string? Password, string? NewPassword);

    public record UpdateUserRequest(string? Id, string? Name, string? PhoneNumber, int? AccountType);

    public record AuthResponse(string Token, UserOut User);
}
