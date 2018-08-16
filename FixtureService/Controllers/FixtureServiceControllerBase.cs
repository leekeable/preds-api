namespace FixtureService.Controllers
{
    using Microsoft.AspNetCore.Mvc;

    public class FixtureServiceControllerBase : ControllerBase
    {
        protected InternalServerErrorObjectResult InternalServerError()
        {
            return new InternalServerErrorObjectResult();
        }

        protected InternalServerErrorObjectResult InternalServerError(object value)
        {
            return new InternalServerErrorObjectResult(value);
        }
    }
}
