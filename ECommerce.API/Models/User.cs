using System;
using System.Collections.Generic;

namespace ECommerce.API.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        public string ModifiedAt { get; set; } = string.Empty;
         

    
        // RBAC-related properties
        public string UserName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Roles { get; set; } = string.Empty;

        public bool IsActive { get; set; }
        public string? Token { get; set; }

        // Constructors
        public User()
        {
        }

        public User(string userName, string name, string password, string roles)
        {
            UserName = userName;
            Name = name;
            Password = password;
            Roles = roles;
        }
    }

    public class LoginUser
    {
        public string UserName { get; set; } = "";
        public string Password { get; set; } = "";
    }


    public class RegisterUser
    {
        public string Name { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string Mobile { get; set; }
        public string Password { get; set; }
        public string Roles { get; set; }
        public int Id { get; set; }
        public string CreatedAt { get; set; }
        public string ModifiedAt { get; set; }
    }
    public class LoginResponse
    {
        public string Token { get; set; }
        public string Role { get; set; }
    }

    public class LoginResult
    {
        public string Token { get; set; }
        public string Roles { get; set; }
    }
}
