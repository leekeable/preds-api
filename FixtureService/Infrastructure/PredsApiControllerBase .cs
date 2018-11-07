namespace FixtureService.Infrastructure
{
    using Microsoft.AspNetCore.Mvc;
    using System.Security.Claims;

    public class PredsApiControllerBase : ControllerBase
    {
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
    }
}
