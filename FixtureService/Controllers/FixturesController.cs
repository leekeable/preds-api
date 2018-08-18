namespace FixtureService.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Caching.Memory;
    using NLog;
    using System;
    using System.Collections.Generic;

    [Route("/")]
    [ApiController]
    public class FixturesController : FixtureServiceControllerBase
    {
        private IMemoryCache _cache;
        private Logger logger = LogManager.GetCurrentClassLogger();
        public FixturesController(IMemoryCache memoryCache)
        {
            _cache = memoryCache;
        }

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

        private ActionResult<IEnumerable<Fixture>> Get(string teamname)
        {
            IEnumerable<Fixture> fixtures = null;
            try
            {
                if (!_cache.TryGetValue(teamname, out fixtures))
                {
                    logger.Info($"{teamname} not cached");
                    IFixtureParser parser = new SkyResultParser($"http://www.skysports.com/{teamname}");
                    var results = parser.GetFixtures();
                    if (results.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        // Set cache options.
                        var cacheEntryOptions = new MemoryCacheEntryOptions()

                        // Keep in cache for this time
                            .SetAbsoluteExpiration(TimeSpan.FromHours(12));

                        // Save data in cache.
                        _cache.Set(teamname, results.Fixtures, cacheEntryOptions);
                        logger.Info($"{teamname} added to cache");
                        return Ok(results.Fixtures);
                    }
                    else
                    {
                        return NotFound();
                    }
                }
                logger.Info($"{teamname} returned from cache");
                return Ok(fixtures);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}
