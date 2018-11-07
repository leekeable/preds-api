namespace FixtureService.Services
{
    using FixtureService.Models;
    using System.Collections.Generic;

    public interface ILeagueTableService
    {
        IEnumerable<LeagueTableItem> GetLeagueTable(string userName);
    }
}
