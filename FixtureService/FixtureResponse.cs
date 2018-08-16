using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FixtureService
{
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
