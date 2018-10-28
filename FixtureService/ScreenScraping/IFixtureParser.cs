namespace FixtureService.ScreenScraping
{
    using FixtureService.Models;

    public interface IFixtureParser
    {
        FixtureResponse GetFixtures();
    }
}
