namespace FixtureService.Infrastructure
{
    using FixtureService.Models;
    using System.Collections.Generic;

    public interface IDataContext
    {
        #region Player
        List<Player> GetPlayers();
        Player GetPlayer(string name);
        #endregion

        #region Fixtures
        int GetWeek();
        IEnumerable<Fixture> GetWeeksFixtures(int week);
        #endregion

        #region Teams
        Team GetTeam(string name);
        Competition GetCompetition(int id);
        Competition GetCompetition(string name);
        #endregion

        #region Security

        bool IsValidUsernameAndPassword(string userName, string password);
        #endregion

        #region Account
        bool UpdateEmailAddress(string userName, string email);
        bool ChangePassword(string userName, string newPassword);
        #endregion
    }
}
