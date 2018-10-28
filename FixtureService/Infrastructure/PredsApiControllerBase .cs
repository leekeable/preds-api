namespace FixtureService.Infrastructure
{
    using Microsoft.AspNetCore.Mvc;

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
    }
}
