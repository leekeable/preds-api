namespace FixtureService.Controllers
{
    using FixtureService.Infrastructure;
    using FixtureService.Models;
    using FixtureService.ScreenScraping;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Configuration;
    using NLog;
    using System;
    using System.Collections.Generic;
    using System.Data.SqlClient;

    [Route("/")]
    [ApiController]
    public class PredsApiController : PredsApiControllerBase
    {
        private IMemoryCache _cache;
        private Logger logger = LogManager.GetCurrentClassLogger();
        private readonly string _connectionString;
        public PredsApiController(IMemoryCache memoryCache, IConfiguration configuration)
        {
            _cache = memoryCache;
            _connectionString = configuration.GetConnectionString("FootyPreds");
        }

        [HttpGet]
        [Route("leaguetable")]
        public ActionResult<IEnumerable<LeagueTableItem>> LeagueTable()
        {
            List<LeagueTableItem> league;

            if (!_cache.TryGetValue("leaguetable", out league))
            {
                // this is way too slow
                List<Player> players = GetPlayers();
                league = new List<LeagueTableItem>();
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    int pld = 0;
                    using (var command = connection.CreateCommand())
                    {
                        // games played
                        var playedSql = "SELECT COUNT(Id) FROM Results";
                        command.CommandText = playedSql;
                        pld = (int)command.ExecuteScalar();
                    }

                    foreach (Player p in players)
                    {
                        using (var command = connection.CreateCommand())
                        {
                            var predictedSql = $"SELECT COUNT(FixtureId) FROM Predictions WHERE Predictions.PlayerId = {p.Id}";
                            // Home Wins
                            var homewinsSql = $"SELECT COUNT(FixtureId) FROM Predictions, Results WHERE Predictions.FixtureId = Results.Id AND Predictions.PlayerId = {p.Id} AND Predictions.HomeScore > Predictions.AwayScore AND Results.HomeScore > Results.AwayScore";
                            // Away wins
                            var awaywinsSql = $"SELECT COUNT(FixtureId) FROM Predictions, Results WHERE Predictions.FixtureId = Results.Id AND Predictions.PlayerId = {p.Id} AND Predictions.HomeScore < Predictions.AwayScore AND Results.HomeScore < Results.AwayScore";
                            // draws 
                            var drawsSql = $"SELECT COUNT(FixtureId) FROM Predictions, Results WHERE Predictions.FixtureId = Results.Id AND Predictions.PlayerId = {p.Id} AND Predictions.HomeScore = Predictions.AwayScore AND Results.HomeScore = Results.AwayScore";
                            // number of scores
                            var scoresSql = $"SELECT COUNT(FixtureId) FROM Predictions, Results WHERE Predictions.FixtureId = Results.Id AND Predictions.PlayerId = {p.Id} AND Predictions.HomeScore = Results.HomeScore AND Predictions.AwayScore = Results.AwayScore";
                            // calculate bonus points....
                            var nogoalsSql = $"SELECT COUNT(FixtureId) FROM Predictions, Results WHERE Predictions.FixtureId = Results.Id AND Predictions.PlayerId = {p.Id} AND Predictions.HomeScore = Results.HomeScore AND Predictions.AwayScore = Results.AwayScore AND (Predictions.HomeScore + Predictions.AwayScore) = 0";
                            var threegoalsSql = $"SELECT COUNT(FixtureId) FROM Predictions, Results WHERE Predictions.FixtureId = Results.Id AND Predictions.PlayerId = {p.Id} AND Predictions.HomeScore = Results.HomeScore AND Predictions.AwayScore = Results.AwayScore AND (Predictions.HomeScore + Predictions.AwayScore) > 3 AND (Predictions.HomeScore + Predictions.AwayScore) <= 6";
                            var sevengoalsSql = $"SELECT COUNT(FixtureId) FROM Predictions, Results WHERE Predictions.FixtureId = Results.Id AND Predictions.PlayerId = {p.Id} AND Predictions.HomeScore = Results.HomeScore AND Predictions.AwayScore = Results.AwayScore AND (Predictions.HomeScore + Predictions.AwayScore) > 6";

                            var starsSql = $"SELECT TournamentName, ImageUrl FROM Winners WHERE WinnerName = '{p.NickName}'";

                            command.CommandText = predictedSql;
                            var prds = (int)command.ExecuteScalar();

                            command.CommandText = homewinsSql;
                            var res = (int)command.ExecuteScalar();

                            command.CommandText = awaywinsSql;
                            res += (int)command.ExecuteScalar();

                            command.CommandText = drawsSql;
                            res += (int)command.ExecuteScalar();

                            command.CommandText = scoresSql;
                            var scrs = (int)command.ExecuteScalar();

                            res = res - scrs;

                            command.CommandText = nogoalsSql;
                            var nogoals = (int)command.ExecuteScalar();

                            command.CommandText = threegoalsSql;
                            var threegoals = (int)command.ExecuteScalar();

                            command.CommandText = sevengoalsSql;
                            var sevengoals = (int)command.ExecuteScalar();

                            command.CommandText = starsSql;
                            var starsRdr = command.ExecuteReader();

                            var starsList = new List<Trophy>();

                            while (starsRdr.Read())
                            {
                                starsList.Add(new Trophy((string)starsRdr["TournamentName"], (string)starsRdr["ImageUrl"]));
                            }

                            var bonus = nogoals + (threegoals * 2) + (sevengoals * 3);
                            var total = res + (scrs * 3) + bonus;

                            var lti = new LeagueTableItem(p.NickName, starsList, pld, res, scrs, bonus, total);
                            league.Add(lti);
                        }
                    }
                }
                league.Sort();

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromHours(1));
                _cache.Set("leaguetable", league, cacheEntryOptions);
            }
            return Ok(league);
        }

        private List<Player> GetPlayers()
        {
            var players = new List<Player>();
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT Id, Name, NickName, email, IsAdmin FROM Players";
                    var playerRdr = command.ExecuteReader();
                    while (playerRdr.Read())
                    {
                        players.Add(new Player(playerRdr.GetInt32(playerRdr.GetOrdinal("Id")), playerRdr.GetString(playerRdr.GetOrdinal("Name")), playerRdr.GetString(playerRdr.GetOrdinal("NickName")), playerRdr.GetString(playerRdr.GetOrdinal("email")), playerRdr.GetBoolean(playerRdr.GetOrdinal("IsAdmin"))));
                    }
                }
            }
            return players;
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
