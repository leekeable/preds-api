namespace FixtureService.Infrastructure
{
    using FixtureService.Models;
    using System.Collections.Generic;

    public interface IDataContext
    {
        bool IsValidUsernameAndPassword(string userName, string password);
        List<Player> GetPlayers();
        Player GetPlayer(string name);
        int GetWeek();
        IEnumerable<LeagueTableItem> GetLeagueTable(string userName);
    }
}
