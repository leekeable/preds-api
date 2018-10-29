namespace FixtureService.Controllers
{
    using FixtureService.Infrastructure;
    using FixtureService.Models;
    using FixtureService.ScreenScraping;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Configuration;
    using NLog;
    using System;
    using System.Collections.Generic;

    [Route("/")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class PredsApiController : PredsApiControllerBase
    {
        private IMemoryCache _cache;
        private Logger logger = LogManager.GetCurrentClassLogger();
        private IDataContext _context;
        private IFixtureParser _parser;// = new SkyResultParser();
        public PredsApiController(IMemoryCache memoryCache, IConfiguration configuration, IDataContext context, IFixtureParser parser) : base(configuration, context)
        {
            _cache = memoryCache;
            _context = context;
            _parser = parser;
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("createtoken")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public IActionResult CreateToken([FromBody] LoginModel login)
        {
            if (context.IsValidUsernameAndPassword(login.UserName, login.Password))
            {
                return Ok(this.GenerateToken(login));
            }
            return BadRequest();
        }

        [HttpGet]
        [Route("upcomingfixtures")]
        [Authorize(Roles = "Admin")]
        public ActionResult<IEnumerable<Fixture>> UpcomingFixtures()
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

            if (!_cache.TryGetValue("leaguetable", out league))
            {
                league = context.GetLeagueTable(username);

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromHours(1));
                _cache.Set("leaguetable", league, cacheEntryOptions);
            }
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
        public ActionResult<IEnumerable<Fixture>> GetResultsFixtures(string teamname)
        {
            var skypath = teamname.ToLower().Replace(".", "-") + "-results";
            return RetrieveFixtures(skypath);
        }
        private ActionResult<IEnumerable<Fixture>> RetrieveFixtures(string teamname)
        {
            IEnumerable<Fixture> fixtures = null;
            try
            {
                if (!_cache.TryGetValue(teamname, out fixtures))
                {
                    logger.Info($"{teamname} not cached");
                    
                    var results = _parser.GetFixtures($"http://www.skysports.com/{teamname}");
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
