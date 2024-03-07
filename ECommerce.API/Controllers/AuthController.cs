using ECommerce.API.Models;
using ECommerce.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace ECommerce.API.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // POST: auth/login
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginUser user)
        {
            // Error checks

            if (String.IsNullOrEmpty(user.UserName))
            {
                return BadRequest(new { message = "User name needs to entered" });
            }
            else if (String.IsNullOrEmpty(user.Password))
            {
                return BadRequest(new { message = "Password needs to entered" });
            }

            // Try login

            var loggedInUser = await _authService.Login(new User(user.UserName, "", user.Password,""));

            // Return responses

            if (loggedInUser != null)
            {
                return Ok(loggedInUser);
            }

            return BadRequest(new { message = "User login unsuccessful" });
        }
[AllowAnonymous]
[HttpPost]
public async Task<IActionResult> Register([FromBody] RegisterUser user)
{
    // Error checks
    if (String.IsNullOrEmpty(user.Name))
    {
        return BadRequest(new { message = "Name needs to be entered" });
    }
    else if (String.IsNullOrEmpty(user.UserName))
    {
        return BadRequest(new { message = "User name needs to be entered" });
    }
    else if (String.IsNullOrEmpty(user.Password))
    {
        return BadRequest(new { message = "Password needs to be entered" });
    }
    else if (String.IsNullOrEmpty(user.Email))
    {
        return BadRequest(new { message = "Email needs to be entered" });
    }

    // Try registration
    var registeredUser = await _authService.Register(new User
    {
        UserName = user.UserName,
        Name = user.Name,
        Password = user.Password,
        Email = user.Email,
        Address = user.Address,
        Mobile = user.Mobile,
        Roles = user.Roles,
        Id = user.Id,
        CreatedAt = user.CreatedAt,
        ModifiedAt = user.ModifiedAt
    });

    // Return responses
    if (registeredUser != null)
    {
        return Ok(registeredUser);
    }

    return BadRequest(new { message = "User registration unsuccessful", error = "Failed to insert the user into the database" });
}

        // GET: auth/test
        [Authorize(Roles = "User")]
        [HttpGet]
        public IActionResult Test()
        {
            // Get token from header

            string token = Request.Headers["Authorization"];

            if (token.StartsWith("Bearer"))
            {
                token = token.Substring("Bearer ".Length).Trim();
            }
            var handler = new JwtSecurityTokenHandler();

            // Returns all claims present in the token

            JwtSecurityToken jwt = handler.ReadJwtToken(token);

            var claims = "List of Claims: \n\n";

            foreach (var claim in jwt.Claims)
            {
                claims += $"{claim.Type}: {claim.Value}\n";
            }

            return Ok(claims);
        }
    }
}

