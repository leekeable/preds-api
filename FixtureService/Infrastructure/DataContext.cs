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

        /// <summary>
        /// Gets the weeks fixtures.
        /// </summary>
        /// <param name="week">The week.</param>
        /// <returns></returns>
        public IEnumerable<Fixture> GetWeeksFixtures(int week)
        {
            var fixtures = new List<Fixture>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"SELECT Id FROM Fixtures WHERE Week = {week} ORDER BY Date ASC";
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        var fixture = GetFixture(reader.GetInt64(reader.GetOrdinal("Id")));
                        fixtures.Add(fixture);
                    }
                }
                return fixtures;
            }
        }

        /// <summary>
        /// Gets the team.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public Team GetTeam(string name)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"SELECT Id, Url FROM TeamUrl WHERE TeamName = '{name}'";
                    var reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        return new Team
                        {
                            Name = name,
                            Url = reader.GetString(reader.GetOrdinal("Url")),
                            Badge = BuildBadgePath(reader.GetInt32(reader.GetOrdinal("Id")))
                        };
                    }
                    else
                    {
                        return new Team
                        {
                            Name = name
                        };
                    }
                }
            }
        }

        /// <summary>
        /// Builds the badge path.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        private string BuildBadgePath(int id)
        {
            var path = string.Format(configuration["Badges"], id);
            return path;
        }

        /// <summary>
        /// Gets the fixture.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns></returns>
        public Fixture GetFixture(long id)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"SELECT Fixtures.HomeTeam, Fixtures.AwayTeam, Fixtures.Date, Fixtures.CompetitionId, Fixtures.Week, Results.HomeScore, Results.AwayScore FROM Fixtures LEFT JOIN Results ON Fixtures.Id = Results.Id WHERE Fixtures.Id = {id}";
                    var reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        var c = GetCompetition(reader.GetInt32(reader.GetOrdinal("CompetitionId")));

                        var homeTeam = GetTeam(reader.GetString(reader.GetOrdinal("HomeTeam")));
                        var awayTeam = GetTeam(reader.GetString(reader.GetOrdinal("AwayTeam")));

                        var date = reader.GetDateTime(reader.GetOrdinal("Date"));
                        int? week = null;
                        if (!reader.IsDBNull(reader.GetOrdinal("Week")))
                        {
                            week = reader.GetInt32(reader.GetOrdinal("Week"));
                        }

                        int? homescore = null;
                        if (!reader.IsDBNull(reader.GetOrdinal("HomeScore")))
                        {
                            homescore = reader.GetInt32(reader.GetOrdinal("HomeScore"));
                        }

                        int? awayscore = null;
                        if (!reader.IsDBNull(reader.GetOrdinal("AwayScore")))
                        {
                            awayscore = reader.GetInt32(reader.GetOrdinal("AwayScore"));
                        }

                        return new Fixture
                        {
                            Kickoff = date,
                            HomeTeam = homeTeam,
                            AwayTeam = awayTeam,
                            HomeScore = homescore,
                            AwayScore = awayscore,
                            Competition = c,
                            Id = id
                        };
                    }
                    return null;
                }
            }
        }

        public Competition GetCompetition(string name)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"SELECT Description, AlternateDesc, url FROM Competitions WHERE (Description = '{name}') OR (AlternateDesc = '{name}'";
                    var reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        return new Competition
                        {
                            Name = reader.GetString(reader.GetOrdinal("Description")),
                            Url = reader.GetString(reader.GetOrdinal("Url"))
                        };
                    }
                    return null;
                }
            }

        }

        public Competition GetCompetition(int id)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"SELECT Description, url FROM Competitions WHERE Id = {id}";
                    var reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        return new Competition
                        {
                            Name = reader.GetString(reader.GetOrdinal("Description")),
                            Url = reader.GetString(reader.GetOrdinal("Url"))
                        };
                    }
                    return null;
                }
            }
        }



        /// <summary>
        /// Updates the email address.
        /// </summary>
        /// <param name="userName">Name of the user.</param>
        /// <param name="email">The email.</param>
        public bool UpdateEmailAddress(string userName, string email)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"UPDATE Players SET Email = '{email}' WHERE NickName = '{userName}'";
                    command.ExecuteNonQuery();
                    return true;
                }
            }
        }

        /// <summary>
        /// Changes the password.
        /// </summary>
        /// <param name="userName">Name of the user.</param>
        /// <param name="newPassword">The new password.</param>
        /// <returns></returns>
        public bool ChangePassword(string userName, string newPassword)
        {
            //if (IsValidUsernameAndPassword(userName, currentpassword))
            //{
            try
            {
                var crypto = new SimpleCrypto.PBKDF2();
                var hashedPassword = crypto.Compute(newPassword);
                var salt = crypto.Salt;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = $"UPDATE Players SET Password = '{hashedPassword}', PasswordSalt = '{salt}' WHERE NickName = '{userName}'";
                        command.ExecuteNonQuery();
                        return true;
                    }
                }
            }
            catch
            {
                // do some logging
                return false;
            }
            //}
            //return false;
        }
    }
}
