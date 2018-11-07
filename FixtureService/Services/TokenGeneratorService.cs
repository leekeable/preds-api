namespace FixtureService.Services
{
    using FixtureService.Infrastructure;
    using FixtureService.Models;
    using Microsoft.Extensions.Configuration;
    using Microsoft.IdentityModel.Tokens;
    using System;
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Text;

    public class TokenGeneratorService : ITokenGeneratorService
    {
        private readonly IDataContext context;
        private readonly IConfiguration configuration;

        public TokenGeneratorService(IDataContext context, IConfiguration configuration)
        {
            this.context = context;
            this.configuration = configuration;
        }

        // Token generation should be in a service that depends on the DataContext - but i'm feeling lazy
        public string GenerateToken(LoginModel login)
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
