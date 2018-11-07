namespace FixtureService.Models
{
    using Newtonsoft.Json;
    using System;

    public class Fixture
    {
        public long Id { get; set; }
        public Team HomeTeam { get; set; }
        public Team AwayTeam { get; set; }
        public Competition Competition { get; set; }
        public DateTime Kickoff { get; set; }
        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }
        [JsonIgnore]
        public bool HasResult
        {
            get
            {
                return HomeScore.HasValue && AwayScore.HasValue;
            }
        }
    }
}