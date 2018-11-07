namespace FixtureService.Controllers
{
    using FixtureService.Infrastructure;
    using FixtureService.Models;
    using FixtureService.Services;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using NLog;
    using System.Collections.Generic;

    [Route("/")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class AccountController : PredsApiControllerBase
    {
        private Logger logger = LogManager.GetCurrentClassLogger();
        private readonly IDataContext context;
        private readonly ITokenGeneratorService tokenGeneratorService;
        public AccountController(IDataContext context, ITokenGeneratorService tokenGeneratorService)
        {
            this.context = context;
            this.tokenGeneratorService = tokenGeneratorService;
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("createtoken")]
        public IActionResult CreateToken([FromBody] LoginModel login)
        {
            if (!context.IsValidUsernameAndPassword(login.UserName, login.Password))
            {
                return BadRequest();
            }
            logger.Debug($"Token created for {login.UserName}");
            return Ok(tokenGeneratorService.GenerateToken(login));

        }

        [HttpPut]
        [Route("updateemail")]
        public ActionResult<IEnumerable<Fixture>> PutUpdateEmail(string email)
        {
            var username = GetUserName();
            if (!context.UpdateEmailAddress(username, email))
            {
                return BadRequest();
            }
            return Ok();
        }

        [HttpPut]
        [Route("changepassword")]
        public ActionResult<IEnumerable<Fixture>> PutChangePassword(string newPassword)
        {
            var username = GetUserName();
            if (!context.ChangePassword(username, newPassword))
            {
                return BadRequest();
            }
            return Ok();
        }
    }
}
