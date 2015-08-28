using HtmlAgilityPack;
using ScrapySharp.Extensions;
using System;
using System.Collections.Generic;
//using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TGLibrary.TGConsole;

namespace DimmiScraper
{
    [Serializable]
    class City
    {
        private string URLBase;
        private string URLPage1;
        const byte RESTAURANTS_PER_PAGE = 30;
        public List<Restaurant> Restaurants;

        public City(string url)
        {
            URLBase = url;
            URLPage1 = URLBase + @"?page=1";
        }

        public void getRestaurants()
        {
            HtmlNode docNode = getDocumentNode(URLPage1);
            int NumRestaurants = getNumberofRestaurants(docNode);
            docNode = null;

            if (NumRestaurants > 0)
            {
                Console.WriteLine("Getting {0} Restaurants...", NumRestaurants);
                Restaurants = new List<Restaurant>(NumRestaurants);
                byte numPages = (byte)(NumRestaurants / RESTAURANTS_PER_PAGE);
                if ((NumRestaurants % RESTAURANTS_PER_PAGE) != 0) numPages = (byte)(numPages + 1);

                ProgressBar pb = new ProgressBar(numPages);
                for (int pageCount = 0; pageCount < numPages; pageCount++)
                {
                    Console.Write(pb.Update(pageCount+1));
                    var currentPage = getDocumentNode(URLBase + @"?page=" + (pageCount + 1).ToString());
                    var allRestaurantsOnPage = currentPage.CssSelect("#search-results > article").ToArray();

                    byte onPage = (byte)allRestaurantsOnPage.Length;

                    Task[] resTasks = new Task[onPage];
                    List<Restaurant> resOut = new List<Restaurant>(onPage);
                    for (int currentRestaurantCounter = 0; currentRestaurantCounter < onPage; currentRestaurantCounter += 1)
                    {
                        var currentRestaurantNode = allRestaurantsOnPage[currentRestaurantCounter];
                        resTasks[currentRestaurantCounter] = Task.Factory.StartNew(() =>
                        {
                            Restaurant currentRestaurant = processRestaurant(currentRestaurantNode);
                            if (currentRestaurant != null) { resOut.Add(currentRestaurant); }
                        });
                    }
                    Task.WaitAll(resTasks);

                    Restaurants.AddRange(resOut);
                }
            }
        }

        private int getNumberofRestaurants(HtmlNode docNode)
        {
            string fullString = docNode.CssSelect("h1.autocomplete-text").Single().InnerText.Trim();
            string removeBefore = docNode.CssSelect("h1.autocomplete-text > .book").Single().InnerText;
            string removeAfter = " Restaurants  in " + docNode.CssSelect("h1.autocomplete-text > strong").Single().InnerText;
            fullString = fullString.Substring(removeBefore.Length).Trim();
            fullString = fullString.Remove(fullString.Length - removeAfter.Length);

            int NumRestaurants;
            if (int.TryParse(fullString, out NumRestaurants) == false) NumRestaurants = -1;

            return NumRestaurants;
        }

        private Restaurant processRestaurant(HtmlNode current)
        {
            string PrimaryKey = current.CssSelect(".image.cell > a").Single().GetAttributeValue("href").Substring(@"/restaurant/".Length).Trim();

            string Name = current.CssSelect(".restaurant-name").Single().InnerText.Trim();

            string Score;
            string NumberOfReviews;
            HtmlNode scoreNode = current.CssSelect("dl.details.score > dd").Single();
            HtmlNode scoreNodeTotal = scoreNode.CssSelect(".score-total").FirstOrDefault();
            if (scoreNodeTotal != default(HtmlNode))
            {
                Score = scoreNodeTotal.InnerText.Trim();
                Score = Score.Substring(1);
                Score = Score.Substring(0, Score.Length - 1);

                NumberOfReviews = scoreNode.CssSelect(".score-description > a").Single().InnerText.Trim();
                NumberOfReviews = NumberOfReviews.Remove(NumberOfReviews.Length - " reviews".Length);
            }
            else
            {
                Score = "NULL";
                NumberOfReviews = "0";
            }            

            string Cuisine = current.CssSelect(".details.cuisine > dd").Single().InnerText.Trim();
            if (Cuisine.ToUpper() == "BREAKFAST" || Cuisine.ToUpper() == "CAFE") return null;

            string BestFor = current.CssSelect(".details.best-for > dd").Single().InnerText.Trim();

            if (BestFor.ToUpper() == "NOT AVAILABLE")
            {
                BestFor = "NULL";
            }

            string AvgSpend = current.CssSelect(".details.spend > dd").Single().InnerText.Trim();
            if (AvgSpend.ToUpper() != "N/A")
            {
                AvgSpend = AvgSpend.Substring(1);
                AvgSpend = AvgSpend.Remove(AvgSpend.Length - "  per person".Length).Trim();
            }
            else
            {
                AvgSpend = "NULL";
            }

            return new Restaurant(PrimaryKey, Name, Score, NumberOfReviews, Cuisine, BestFor, AvgSpend);
        }

        private static HtmlNode getDocumentNode(string URL)
        {
            HtmlNode docNode = null;
            using (var web = new CompressedWebClient())
            {
                bool tryAgain = true;
                string currentFile = null;
                while(tryAgain)
                {
                    try
                    {
                        currentFile = web.DownloadString(URL);
                        tryAgain = false;
                    }
                    catch (Exception ex)
                    {
                        System.Threading.Thread.Sleep(10000);
                    }
                }                
                
                if (web.StatusCode() == System.Net.HttpStatusCode.OK)
                {
                    var doc = new HtmlDocument();
                    doc.LoadHtml(currentFile);
                    docNode = doc.DocumentNode;
                }
                else
                {
                    throw404(URL);
                }
            }
            return docNode;
        }

        #region Exceptions
        private static void throw404(string url)
        {
            throw new System.Web.HttpException(404, string.Format("Webpage did not respond [{0}]", url));
        }
        #endregion
    }
}
