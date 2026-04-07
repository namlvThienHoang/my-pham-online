namespace BeautyEcommerce.Api.Controllers;

using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using BeautyEcommerce.Application.Features.Auth.Commands.Register;
using BeautyEcommerce.Application.Features.Auth.Commands.Login;
using BeautyEcommerce.Application.Features.Auth.Commands.RefreshToken;
using BeautyEcommerce.Application.Features.Auth.Commands.EnableMfa;
using BeautyEcommerce.Application.Features.Auth.Commands.VerifyMfa;
using BeautyEcommerce.Application.Features.Auth.Commands.Logout;
using BeautyEcommerce.Application.Features.Auth.Queries.GetCurrentUser;
using BeautyEcommerce.Application.Features.Auth.DTOs;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        var command = new RegisterCommand
        {
            Email = request.Email,
            Password = request.Password,
            FullName = request.FullName,
            PhoneNumber = request.PhoneNumber,
            IpAddress = GetClientIpAddress(),
            UserAgent = Request.Headers.UserAgent.ToString()
        };

        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error, code = result.ErrorCode });
        }

        return Ok(result.Value);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var command = new LoginCommand
        {
            Email = request.Email,
            Password = request.Password,
            MfaCode = request.MfaCode,
            IpAddress = GetClientIpAddress(),
            UserAgent = Request.Headers.UserAgent.ToString()
        };

        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "MFA_REQUIRED")
            {
                return StatusCode(428, new { message = result.Error, requiresMfa = true });
            }
            return Unauthorized(new { message = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request)
    {
        var command = new RefreshTokenCommand
        {
            RefreshToken = request.RefreshToken,
            IpAddress = GetClientIpAddress(),
            UserAgent = Request.Headers.UserAgent.ToString()
        };

        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
        {
            return Unauthorized(new { message = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpPost("enable-mfa")]
    [Authorize]
    public async Task<IActionResult> EnableMfa()
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized();
        }

        var command = new EnableMfaCommand { UserId = userId };
        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpPost("verify-mfa")]
    [Authorize]
    public async Task<IActionResult> VerifyMfa([FromBody] VerifyMfaRequestDto request)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized();
        }

        var command = new VerifyMfaCommand { UserId = userId, Code = request.Code };
        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error });
        }

        return Ok(new { success = true });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] LogoutRequestDto? request)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized();
        }

        var command = new LogoutCommand 
        { 
            UserId = userId, 
            RefreshToken = request?.RefreshToken,
            LogoutAllDevices = request == null
        };
        
        await _mediator.Send(command);
        
        return Ok(new { success = true });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized();
        }

        var query = new GetCurrentUserQuery { UserId = userId };
        var result = await _mediator.Send(query);
        
        if (!result.IsSuccess)
        {
            return NotFound(new { message = result.Error });
        }

        return Ok(result.Value);
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null || !Guid.TryParse(claim.Value, out var userId))
        {
            return Guid.Empty;
        }
        return userId;
    }

    private string? GetClientIpAddress()
    {
        var ip = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (string.IsNullOrEmpty(ip))
        {
            ip = Request.HttpContext.Connection.RemoteIpAddress?.ToString();
        }
        return ip;
    }
}
