namespace FixtureService.ScreenScraping
{
    using FixtureService.Models;
    using HtmlAgilityPack;
    using NLog;
    using System;
    using System.Globalization;
    public class SkyResultParser : IFixtureParser
    {
        private Logger logger = LogManager.GetCurrentClassLogger();

        public FixtureResponse GetFixtures(string Url)
        {

            var ret = new FixtureResponse();

            var web = new HtmlWeb();
            
            var input = web.Load(Url);
            if (web.StatusCode != System.Net.HttpStatusCode.OK)
            {
                logger.Error($"{Url} {web.StatusCode}");
                ret.StatusCode = web.StatusCode;
                return ret;
            }

            //Console.WriteLine($"Parsing {t.Name} results");
            var body = input.DocumentNode.SelectSingleNode("//div[@class='fixres__body callfn']");
            if (body == null)
            {
                logger.Info($"{Url} no results nodes found");
                ret.StatusCode = System.Net.HttpStatusCode.NotFound;
                return ret;
            }

            int year = 0;
            DateTime kickoff = DateTime.MinValue;
            string hometeam = string.Empty;
            string awayteam = string.Empty;
            string competition = string.Empty;

            foreach (var node in body.ChildNodes)
            {
                try
                {
                    if (node.Name == "h3")
                    {
                        var m = node.InnerText.Split(' ');
                        year = Int32.Parse(m[1]);
                        kickoff = DateTime.MinValue;
                        competition = string.Empty;
                        hometeam = string.Empty;
                        awayteam = string.Empty;
                        logger.Debug($"New result found {node.OuterHtml}");
                    }
                    else if (node.Name == "h4")
                    {
                        kickoff = GetKickOff(node, year);
                        hometeam = string.Empty;
                        awayteam = string.Empty;
                        logger.Debug($"Kickoff found {node.OuterHtml}");
                    }
                    else if (node.Name == "h5")
                    {
                        // on a league fixtures page, the competition is in the page title
                        competition = GetCompetition(node);
                        logger.Debug($"Competition found {node.OuterHtml}");
                    }
                    else if (node.Name == "div")
                    {
                        hometeam = GetHomeTeam(node);
                        awayteam = GetAwayTeam(node);

                        var status = GetStatus(node);
                        var id = GetId(node);

                        logger.Debug($"Teams and scores found {node.OuterHtml}");

                        if (hometeam != string.Empty && awayteam != string.Empty && kickoff != DateTime.MinValue)
                        {
                            // add to results
                            var f = new Fixture
                            {
                                Id = id,
                                HomeTeam = hometeam,
                                AwayTeam = awayteam,
                                Kickoff = kickoff,
                                Competition = competition
                            };
                            if (status.ToUpper() == "FT")
                            {
                                f.HomeScore = GetHomeScore(node);
                                f.AwayScore = GetAwayScore(node);
                            }

                            ret.Fixtures.Add(f);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                    ret.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                }
            }

            ret.StatusCode = System.Net.HttpStatusCode.OK;
            return ret;
        }

        private int? GetAwayScore(HtmlNode p)
        {
            int? ret = null;
            var awayscore = p.SelectSingleNode("a/span[3]/span[1]/span[2]");
            if (awayscore == null)
            {
                awayscore = p.SelectSingleNode("div/span[3]/span[1]/span[2]");
            }

            if (awayscore != null)
            {
                ret = int.Parse(awayscore.InnerText.Trim());
            }

            return ret;
        }

        private string GetStatus(HtmlNode p)
        {
            var statusNode = p.SelectSingleNode("//*[@data-status]");
            var statattrib = statusNode.Attributes["data-status"].Value;
            return statattrib;
        }

        private int GetId(HtmlNode p)
        {
            var statusNode = p.SelectSingleNode("//*[@data-item-id]");
            var statattrib = statusNode.Attributes["data-item-id"].Value;
            return int.Parse(statattrib);
        }

        private string GetAwayTeam(HtmlNode p)
        {
            var team = p.SelectSingleNode("a/span[4]/span");
            if (team == null)
            {
                team = p.SelectSingleNode("div/span[4]/span");
            }
            return team != null ? team.InnerText.Trim() : string.Empty;
        }

        private string GetCompetition(HtmlNode p)
        {
            return p.InnerText.Trim();
        }

        private int? GetHomeScore(HtmlNode p)
        {
            int? ret = null;
            var homescore = p.SelectSingleNode("a/span[3]/span[1]/span[1]");
            if (homescore == null)
            {
                homescore = p.SelectSingleNode("div/span[3]/span[1]/span[1]");
            }
            if (homescore != null)
            {
                ret = int.Parse(homescore.InnerText.Trim());
            }
            return ret;
        }

        private string GetHomeTeam(HtmlNode p)
        {
            var team = p.SelectSingleNode("a/span[2]/span");
            if (team == null)
            {
                team = p.SelectSingleNode("div/span[2]/span");
            }
            return team != null ? team.InnerText.Trim() : string.Empty;
        }

        private DateTime GetKickOff(HtmlNode p, int year)
        {
            string date = p.InnerText;
            string[] sFields = date.Split(' ');
            int day = Int32.Parse(sFields[1].Substring(0, (sFields[1].Length - 2)));
            int month = GetMonthIndex(sFields[2]);

            // sky seem to sometimes have a hyperlink and sometimes not!
            var timenode = p.NextSibling.NextSibling.NextSibling.NextSibling.SelectSingleNode("a");
            if (timenode == null)
            {
                timenode = p.NextSibling.NextSibling.NextSibling.NextSibling.SelectSingleNode("div");
            }

            string matchtime = timenode.SelectSingleNode("span[3]/span[2]").InnerText.Trim();

            string[] sTime = matchtime.Split(':');
            int hour = Int32.Parse(sTime[0]);
            int min = Int32.Parse(sTime[1]);

            DateTime d = new DateTime(year, month, day, hour, min, 00);
            return d;
        }


        private static int GetMonthIndex(string month)
        {
            return Array.FindIndex(CultureInfo.CurrentCulture.DateTimeFormat.MonthNames,
                             t => t.Equals(month, StringComparison.CurrentCultureIgnoreCase)) + 1;
        }
    }
}
