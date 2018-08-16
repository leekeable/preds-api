namespace FixtureService.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Collections.Generic;

    [Route("api")]
    [ApiController]
    public class FixturesController : FixtureServiceControllerBase
    {
        [HttpGet]
        [Route("fixtures/{teamname}")]
        public ActionResult<IEnumerable<Fixture>> GetFixtures(string teamname)
        {
            var skypath = teamname.ToLower().Replace(".", "-") + "-fixtures";
            return Get(skypath);
        }

        [HttpGet]
        [Route("results/{teamname}")]
        public ActionResult<IEnumerable<Fixture>> GetResultsFixtures(string teamname)
        {
            var skypath = teamname.ToLower().Replace(".", "-") + "-results";
            return Get(skypath);
        }

        [HttpGet]
        private ActionResult<IEnumerable<Fixture>> Get(string teamname)
        {
            try
            {
                IFixtureParser parser = new SkyResultParser($"http://www.skysports.com/{teamname}");
                var results = parser.GetFixtures();
                if (results.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return Ok(results.Fixtures);
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }


    }
}
