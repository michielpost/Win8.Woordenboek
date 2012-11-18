using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Woordenboek.Services
{
    public static class SearchService
    {

        public static async Task<SearchWord> SearchAsync(string text)
        {
            var history = await HistoryService.GetHistory();
            var item = history.Where(x => x.Word.ToLower() == text.ToLower()).FirstOrDefault();
            if (item != null)
                return item;

            try
            {
                HttpClient client = new HttpClient();

                //HttpResponseMessage response = await client.GetAsync("http://www.vandale.nl/ndc-vzs/search/freeSearch.vdw?page=1&viewCount=20&lang=nn&pattern=" + text);
                HttpResponseMessage response = await client.GetAsync(string.Format("http://www.vandale.nl/opzoeken?pattern={0}&lang=nn", text));

                var result = ParseResult(response, text);

                if(result != null && result.Results != null && result.Results.Count > 0)
                    await HistoryService.SaveSearchWord(result);


                return result;


            }
            catch { }

            return null;

        }



        private static SearchWord ParseResult(HttpResponseMessage response, string text)
        {
            var searchWord = new SearchWord { Word = text };


            string result = string.Empty;

            if (response.IsSuccessStatusCode)
            {
                System.Xml.Linq.XElement MyXElement = System.Xml.Linq.XElement.Parse(response.Content.ReadAsStringAsync().Result);

                var list = (from x in MyXElement.Descendants("result")
                            select new
                            {
                                Main = x.Element("headword").Value,
                                Betekenis = x.Element("article").Value,
                            });

                // DateTime.Now.DayOfWeek == DayOfWeek.
                searchWord.Results = new List<ResultWord>();

                foreach (var item in list)
                {
                    if (!item.Betekenis.ToLower().Contains("van dale"))
                    {
                        result += "<h2>" + item.Main + "</h2>" + item.Betekenis;
                        string wordResult = item.Main;
                        wordResult = ReplaceCijfers(wordResult);
                        wordResult = StripHtml(wordResult);

                        string wordBetekenis = item.Betekenis;

                        wordBetekenis = ReplaceCijfers(wordBetekenis);
                        wordBetekenis = StripHtml(wordBetekenis);

                        searchWord.Results.Add(new ResultWord() { Word = wordResult, Description = wordBetekenis });

                    }
                }

                if (string.IsNullOrEmpty(result))
                    result = "No results found.";
                else if (!string.IsNullOrEmpty(result))
                {

                   
                    
                }

            }
            else
            {
                result = "Unable to contact server.";
                return null;
            }

            return searchWord;
        }

        private static string ReplaceCijfers(string input)
        {
            for (int i = 0; i < 15; i++)
            {
                //meerdere definities
                input = input.Replace("<span class=\"f4\">" + i + "</span>", "\n" + i + " ");

                //Voor het woord, mag weg
                input = input.Replace("<span class=\"ft\">" + i + "</span>", string.Empty);

                //Woord titel
                input = input.Replace("<sup>" + i + "</sup>", " (" + i + ") ");



            }

            return input;
        }

        private static string StripHtml(string input)
        {
            Regex reg = new Regex("<[^>]+>", RegexOptions.IgnoreCase);
            return reg.Replace(input, string.Empty).Trim();
        }
    }
}
