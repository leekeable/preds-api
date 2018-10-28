namespace FixtureService.Models
{
    using Newtonsoft.Json;
    using System;

    public class Fixture
    {
        public int Id { get; set; }
        public string HomeTeam { get; set; }
        public string AwayTeam { get; set; }
        public string Competition { get; set; }
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