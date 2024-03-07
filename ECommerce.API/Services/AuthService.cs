using Isopoh.Cryptography.Argon2;
using ECommerce.API.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ECommerce.API.DataAccess;
using Microsoft.VisualBasic;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace ECommerce.API.Services
{
    public interface IAuthService
    {
        public Task<User> Login(User loginUser);
        public Task<User> Register(User registerUser);
    }

    public class AuthService : IAuthService
    {
        readonly IDataAccess dataAccess;
        private readonly string DateFormat;
        private readonly IConfiguration _configuration;

        public AuthService(IDataAccess dataAccess, IConfiguration configuration)
        {
            this.dataAccess = dataAccess;
            DateFormat = configuration["Constants:DateFormat"];
            _configuration = configuration;
        }

        public async Task<User> Login(User loginUser)
        {
            // Search user in DB and verify password

            User? user = dataAccess.GetUserByUserName(loginUser.UserName);

            if (user == null || Argon2.Verify(user.Password, loginUser.Password) == false)
            {
                return null; //returning null intentionally to show that login was unsuccessful
            }

            if (string.IsNullOrEmpty(user.Roles))
            {
                user.Roles = "User"; // Assign default role as "User"
            }
            // Create JWT token handler and get secret key

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["JWT:SecretKey"]);

            // Prepare list of user claims

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.GivenName, user.Name),
                new Claim(ClaimTypes.Role,user.Roles)
            };
            foreach (var claim in claims)
            {
                Console.WriteLine($"{claim.Type}: {claim.Value}");
            }


            // Create token descriptor

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                IssuedAt = DateTime.UtcNow,
                Issuer = _configuration["JWT:Issuer"],
                Audience = _configuration["JWT:Audience"],
                Expires = DateTime.UtcNow.AddMinutes(100),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            };

            // Create token and set it to user

            var token = tokenHandler.CreateToken(tokenDescriptor);
            user.Token = tokenHandler.WriteToken(token);
            user.IsActive = true;

            return user;
        }

        
        public async Task<User> Register(User registerUser)
        {
            // Add user to DB

            registerUser.Password = Argon2.Hash(registerUser.Password);
            registerUser.CreatedAt = DateTime.Now.ToString(DateFormat);
            registerUser.ModifiedAt = DateTime.Now.ToString(DateFormat);
            Console.Write("email: ", registerUser.Email);
         
            if (string.IsNullOrEmpty(registerUser.Roles))
            {
                registerUser.Roles = "User";
                Console.Write("Custom_role: ", registerUser.Roles);
            }
            Console.WriteLine("role", registerUser.Roles);
            var result = dataAccess.InsertUser(registerUser);
            
            Console.Write("Custom_role: ", registerUser.Roles);
            string? message;
            if (result) message = "inserted";
            else message = "email not available";
            return registerUser;
        }
    }
}
