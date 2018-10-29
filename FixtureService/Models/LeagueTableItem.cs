namespace FixtureService.Models
{
    using System;
    using System.Collections.Generic;

    public class LeagueTableItem : IComparable
    {
        public LeagueTableItem(string name, List<Trophy> trophies, int played, int results, int score, int bonus, int points, bool loggedInUser)
        {
            Name = name;
            Trophies = trophies;
            Played = played;
            Results = results;
            Score = score;
            Bonus = bonus;
            Points = points;
            LoggedInUser = loggedInUser;
        }

        public string Name { get; }
        public List<Trophy> Trophies { get; }
        public int Played { get; }
        public int Results { get; }
        public int Score { get; }
        public int Bonus { get; }
        public int Points { get; }
        public bool LoggedInUser { get; }

        public int CompareTo(object obj)
        {
            var comparable = obj as LeagueTableItem;
            if (comparable == null)
            {
                throw new ArgumentException("Object is not a LeagueTableItem");
            }

            var i = Points - comparable.Points;
            if (i != 0) return -i;

            i = (Results + Score) - (comparable.Results + comparable.Score);
            if (i != 0) return -i;

            i = Score - comparable.Score;
            if (i != 0) return -i;

            i = Bonus - comparable.Bonus;
            if (i != 0) return -i;

            i = String.Compare(Name, comparable.Name, StringComparison.Ordinal);
            return i;
        }
    }

    public class Trophy
    {
        public Trophy(string title, string image)
        {
            Title = title;
            Image = image;
        }
        public string Title { get; set; }
        public string Image { get; set; }
    }
}