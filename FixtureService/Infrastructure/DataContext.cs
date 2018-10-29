namespace FixtureService.Infrastructure
{
    using FixtureService.Models;
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Collections.Generic;
    using System.Data.SqlClient;
    public class DataContext : IDataContext
    {
        protected readonly string connectionString;
        protected IConfiguration configuration;

        public DataContext(IConfiguration configuration)
        {
            this.configuration = configuration;
            connectionString = configuration.GetConnectionString("FootyPreds");
        }
        public bool IsValidUsernameAndPassword(string userName, string password)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"SELECT Password, PasswordSalt FROM Players WHERE NickName = '{userName}'";

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var crypto = new SimpleCrypto.PBKDF2();
                            var salt = reader.GetString(reader.GetOrdinal("PasswordSalt"));
                            var hash = reader.GetString(reader.GetOrdinal("Password"));
                            var hashedPassword = crypto.Compute(password, salt);
                            return crypto.Compare(hash, hashedPassword);
                        }
                        return false;
                    }
                }
            }
        }
        public Player GetPlayer(string name)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"SELECT Id, Name, Nickname, email, IsAdmin FROM Players WHERE Nickname = '{name}'";

                    using (var reader = command.ExecuteReader())
                    {
                        reader.Read();
                        var p = new Player(reader.GetInt32(reader.GetOrdinal("Id")),
                            reader.GetString(reader.GetOrdinal("Name")),
                            reader.GetString(reader.GetOrdinal("NickName")),
                            reader.GetString(reader.GetOrdinal("email")),
                            reader.GetBoolean(reader.GetOrdinal("IsAdmin")));
                        return p;
                    }
                }
            }
        }

        public int GetWeek()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT TOP 1 Week FROM Fixtures WHERE Date > getDate() AND Week IS NOT NULL ORDER BY Week";
                    var round = Convert.ToInt32(command.ExecuteScalar());
                    if (round != 0) return round;
                    command.CommandText = "SELECT MAX(Week) FROM Fixtures";
                    try
                    {
                        round = Convert.ToInt32(command.ExecuteScalar());
                    }
                    catch (Exception)
                    {
                        return 0;
                    }

                    return round;
                }
            }
        }
        public List<Player> GetPlayers()
        {
            var players = new List<Player>();
            using (SqlConnection connection = new SqlConnection(connectionString))
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

        public IEnumerable<LeagueTableItem> GetLeagueTable(string userName)
        {
            List<LeagueTableItem> league;
            // this is way too slow
            List<Player> players = GetPlayers();
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
