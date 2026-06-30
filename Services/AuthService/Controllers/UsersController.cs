using Microsoft.AspNetCore.Mvc;
using SharedModels.Dtos;
using SharedModels.Interfaces;
using AuthService.Services;

namespace AuthService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IAuthService _usersService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IAuthService usersService, ILogger<UsersController> logger)
    {
        _usersService = usersService;
        _logger = logger;
    }

    /// <summary>
    /// User login endpoint
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>JWT token on successful authentication</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<DtoLoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] DtoLoginRequest request)
    {
        _logger.LogInformation("Login attempt for email: {Email}", request.Email);

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = "Email and password are required.",
                Data = null,
                Timestamp = DateTime.UtcNow
            });
        }

        var (success, token, userId, message) = await _usersService.LoginAsync(request.Email, request.Password);

        if (!success)
        {
            _logger.LogWarning("Login failed for email: {Email}", request.Email);
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = message,
                Data = null,
                Timestamp = DateTime.UtcNow
            });
        }

        _logger.LogInformation("Login successful for email: {Email}", request.Email);
        return Ok(new ApiResponse<DtoLoginResponse>
        {
            Success = true,
            Message = message,
            Data = new DtoLoginResponse { Token = token, UserId = userId },
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// User registration endpoint
    /// </summary>
    /// <param name="request">Registration details</param>
    /// <returns>User ID on successful registration</returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] DtoRegisterRequest request)
    {
        _logger.LogInformation("Registration attempt for email: {Email}", request.Email);

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Email and password are required.",
                Data = null,
                Timestamp = DateTime.UtcNow
            });
        }

        var (success, userId, message) = await _usersService.RegisterAsync(
            request.FirstName,
            request.LastName,
            request.Email,
            request.Password,
            request.PhoneNumber ?? string.Empty
        );

        if (!success)
        {
            _logger.LogWarning("Registration failed for email: {Email}", request.Email);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = message,
                Data = null,
                Timestamp = DateTime.UtcNow
            });
        }

        _logger.LogInformation("Registration successful for email: {Email}", request.Email);
        return StatusCode(StatusCodes.Status201Created, new ApiResponse<object>
        {
            Success = true,
            Message = message,
            Data = new { UserId = userId },
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Validate JWT token
    /// </summary>
    /// <param name="request">Token to validate</param>
    /// <returns>User ID and validation result</returns>
    [HttpPost("validate-token")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ValidateToken([FromBody] DtoValidateTokenRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Token))
        {
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = "Token is required.",
                Data = null,
                Timestamp = DateTime.UtcNow
            });
        }

        var result = await _usersService.ValidateTokenAsync(request.Token);

        if (!result.Item1)
        {
            _logger.LogWarning("Token validation failed: {Message}", result.Item3);
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = result.Item3,
                Data = null,
                Timestamp = DateTime.UtcNow
            });
        }

        _logger.LogInformation("Token validated successfully for user: {UserId}", result.Item2);
        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = result.Item3,
            Data = new { UserId = result.Item2 },
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Change user password
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="request">Old and new password</param>
    /// <returns>Success message</returns>
    [HttpPost("{userId}/change-password")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword(int userId, [FromBody] DtoChangePasswordRequest request)
    {
        _logger.LogInformation("Password change attempt for user: {UserId}", userId);

        if (string.IsNullOrWhiteSpace(request.OldPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Old and new passwords are required.",
                Data = null,
                Timestamp = DateTime.UtcNow
            });
        }

        var success = await _usersService.ChangePasswordAsync(userId, request.OldPassword, request.NewPassword);

        if (!success)
        {
            _logger.LogWarning("Password change failed for user: {UserId}", userId);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Password change failed. Please check your old password and try again.",
                Data = null,
                Timestamp = DateTime.UtcNow
            });
        }

        _logger.LogInformation("Password changed successfully for user: {UserId}", userId);
        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Password changed successfully",
            Data = null,
            Timestamp = DateTime.UtcNow
        });
    }
}
