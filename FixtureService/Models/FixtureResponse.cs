namespace FixtureService.Models
{
    using System.Collections.Generic;

    public class FixtureResponse
    {
        public FixtureResponse()
        {
            Fixtures = new List<Fixture>();
        }

        public IList<Fixture> Fixtures { get; set; }
        public System.Net.HttpStatusCode StatusCode { get; set; }
    }
}
