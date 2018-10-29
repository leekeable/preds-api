namespace FixtureService.Infrastructure
{
    using FixtureService.Models;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Microsoft.IdentityModel.Tokens;
    using System;
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Text;

    public class PredsApiControllerBase : ControllerBase
    {
        protected IConfiguration configuration;
        protected IDataContext context;

        public PredsApiControllerBase(IConfiguration configuration, IDataContext context)
        {
            this.configuration = configuration;
            this.context = context;
        }

        protected InternalServerErrorObjectResult InternalServerError()
        {
            return new InternalServerErrorObjectResult();
        }

        protected InternalServerErrorObjectResult InternalServerError(object value)
        {
            return new InternalServerErrorObjectResult(value);
        }
        protected string GetUserName()
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;
            if (identity != null)
            {
                return identity.FindFirst(ClaimTypes.Name).Value;
            }
            return null;
        }
        protected string GenerateToken(LoginModel login)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]));
            var handler = new JwtSecurityTokenHandler();
            var now = DateTime.Now;
            var player = context.GetPlayer(login.UserName);
            var securityToken = handler.CreateToken(new SecurityTokenDescriptor()
            {
                Issuer = "issuer",
                Audience = "",
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
                Subject = new ClaimsIdentity(new Claim[]
                {
                    // get other claims eg email, admin role
                    new Claim(ClaimTypes.Name, login.UserName),
                    new Claim(ClaimTypes.Email, player.Email),
                    new Claim(ClaimTypes.Role, player.IsAdmin ? "Admin" : "Player")
                }),
                Expires = now.AddHours(2),
                NotBefore = now
            });

            var accessToken = "Bearer " + handler.WriteToken(securityToken);
            return accessToken;
        }

    }
}
