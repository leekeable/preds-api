namespace FixtureService.Controllers
{
    using FixtureService.Infrastructure;
    using FixtureService.Models;
    using FixtureService.ScreenScraping;
    using FixtureService.Services;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Configuration;
    using NLog;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    [Route("/")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class PredsApiController : PredsApiControllerBase
    {
        private readonly IMemoryCache cache;
        private Logger logger = LogManager.GetCurrentClassLogger();
        private readonly IDataContext context;
        private readonly IFixtureParser parser;
        private readonly ILeagueTableService leagueTableService;
        public PredsApiController(IMemoryCache cache, IConfiguration configuration, IDataContext context, 
                                  IFixtureParser parser, ILeagueTableService leagueTableService)
        {
            this.cache = cache;
            this.context = context;
            this.parser = parser;
            this.leagueTableService = leagueTableService;
        }

        [HttpGet]
        [Route("fixtures/weeksfixtures")]
        public ActionResult<IEnumerable<Fixture>> GetWeeksFixtures(int week)
        {
            var weeksfixtures = context.GetWeeksFixtures(week);
            if (!weeksfixtures.Any())
            {
                return NotFound();
            }
            return Ok(weeksfixtures);
        }

        [HttpGet]
        [Route("fixtures/week")]
        public ActionResult<int> GetWeek()
        {
            return Ok(context.GetWeek());
        }

        [HttpGet]
        [Route("fixtures/upcomingfixtures")]
        public ActionResult<IEnumerable<Fixture>> GetUpcomingFixtures()
        {
            var username = GetUserName();
            return new List<Fixture>();
        }

        [HttpGet]
        [Route("leaguetable")]
        public ActionResult<IEnumerable<LeagueTableItem>> LeagueTable()
        {
            IEnumerable<LeagueTableItem> league;
            var username = GetUserName();

            if (!cache.TryGetValue("leaguetable", out league))
            {
                league = leagueTableService.GetLeagueTable(username);

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromHours(1));
                cache.Set("leaguetable", league, cacheEntryOptions);
            }
            logger.Debug($"leagueTable returned to {GetUserName()}");
            return Ok(league);
        }

        [HttpGet]
        [Route("fixtures/{teamname}")]
        public ActionResult<IEnumerable<Fixture>> GetFixtures(string teamname)
        {
            var skypath = teamname.ToLower().Replace(".", "-") + "-fixtures";
            return RetrieveFixtures(skypath);
        }

        [HttpGet]
        [Route("results/{teamname}")]
        public ActionResult<IEnumerable<Fixture>> GetResults(string teamname)
        {
            var skypath = teamname.ToLower().Replace(".", "-") + "-results";
            return RetrieveFixtures(skypath);
        }

        private ActionResult<IEnumerable<Fixture>> RetrieveFixtures(string teamname)
        {
            IEnumerable<Fixture> fixtures = null;
            try
            {
                if (!cache.TryGetValue(teamname, out fixtures))
                {
                    logger.Info($"{teamname} not cached");
                    
                    var results = parser.GetFixtures($"http://www.skysports.com/{teamname}");
                    if (results.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        // Set cache options.
                        var cacheEntryOptions = new MemoryCacheEntryOptions()

                        // Keep in cache for this time
                            .SetAbsoluteExpiration(TimeSpan.FromHours(12));

                        // Save data in cache.
                        cache.Set(teamname, results.Fixtures, cacheEntryOptions);
                        logger.Info($"{teamname} added to cache");
                        if (!results.Fixtures.Any())
                        {
                            return BadRequest();
                        }
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
