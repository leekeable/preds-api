namespace FixtureService.Services
{
    using FixtureService.Infrastructure;
    using FixtureService.Models;
    using Microsoft.Extensions.Configuration;
    using System.Collections.Generic;
    using System.Data.SqlClient;

    public class LeagueTableService : ILeagueTableService
    {
        private readonly IDataContext context;
        private readonly IConfiguration configuration;

        public LeagueTableService(IDataContext context, IConfiguration configuration)
        {
            this.context = context;
            this.configuration = configuration;
        }

        IEnumerable<LeagueTableItem> ILeagueTableService.GetLeagueTable(string userName)
        {
            var connectionString = configuration.GetConnectionString("FootyPreds");
            List<LeagueTableItem> league;
            // this is way too slow
            List<Player> players = context.GetPlayers();
            league = new List<LeagueTableItem>();
            using (SqlConnection connection = new SqlConnection(connectionString))
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

                        var lti = new LeagueTableItem(p.NickName, starsList, pld, res, scrs, bonus, total, userName == p.NickName);
                        league.Add(lti);
                    }
                }
            }
            league.Sort();
            return league;
        }
    }
}
