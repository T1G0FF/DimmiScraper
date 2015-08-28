using System;
using HtmlAgilityPack;
using ScrapySharp.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TGLibrary.TGConsole;

namespace DimmiScraper
{
    [Serializable]
    class Restaurant
    {
        public string PrimaryKey;
        public string Name;
        public string Address;             // Need to get from reviews page
        public string Score;
        public string NumberOfReviews;
        public string CuisineType;         // Exclude Cafe and Breakfast
        public string BestFor;
        public string AverageSpend;

        public string reviewsURL { get { return @"https://www.dimmi.com.au/restaurant/" + PrimaryKey + @"/reviews"; } } //?reviewsPage=2
        public List<Review> Reviews;

        private const byte REVIEWS_PER_PAGE = 10;


        public Restaurant(string primaryKey, string name, string score, string numReviews, string cuisine, string bestFor, string averageSpend)
        {
            PrimaryKey = primaryKey;
            Name = name;
            Score = score;
            NumberOfReviews = numReviews;
            CuisineType = cuisine;
            BestFor = bestFor;
            AverageSpend = averageSpend;

            if(NumberOfReviews == "0")
            {
                Address = "NULL";
            }
        }

        public void getReviews()
        {
            int NumReviews;
            if (int.TryParse(NumberOfReviews, out NumReviews) == false) NumReviews = -1;

            if (NumReviews > 0)
            {
                HtmlNode docNode = getDocumentNode(reviewsURL + @"?reviewsPage=1");
                var AddressNodes = docNode.CssSelect(".address > a > span").ToArray();
                docNode = null;

                Address = AddressNodes[1].InnerText.Trim();
                Address = Address.Replace("  ", " ");
                Address = Address.Replace(Environment.NewLine, "");

                Console.WriteLine("Getting {0} Reviews...", NumReviews);
                Reviews = new List<Review>(NumReviews);
                byte numPages = (byte)(NumReviews / REVIEWS_PER_PAGE);
                if ((NumReviews % REVIEWS_PER_PAGE) != 0) numPages = (byte)(numPages + 1);

                ProgressBar pb = new ProgressBar(numPages);
                for (int pageCount = 0; pageCount < numPages; pageCount++)
                {
                    Console.Write(pb.Update(pageCount + 1));
                    var currentPage = getDocumentNode(reviewsURL + @"?reviewsPage=" + (pageCount + 1).ToString());
                    var allReviewsOnPage = currentPage.CssSelect("#diner-reviews-list > article[itemProp='review']").ToArray();

                    byte onPage = (byte)allReviewsOnPage.Length;

                    Task[] revTasks = new Task[onPage];
                    List<Review> revOut = new List<Review>(onPage);
                    for (int currentReviewCounter = 0; currentReviewCounter < onPage; currentReviewCounter += 1)
                    {
                        var currentReviewNode = allReviewsOnPage[currentReviewCounter];
                        string reviewNumber = new string('0', NumberOfReviews.Length);
                        reviewNumber = reviewNumber + ((pageCount * REVIEWS_PER_PAGE) + currentReviewCounter).ToString();
                        reviewNumber = reviewNumber.Substring(reviewNumber.Length - NumberOfReviews.Length);

                        revTasks[currentReviewCounter] = Task.Factory.StartNew(() =>
                        {
                            Review currentReview = processReview(currentReviewNode, reviewNumber);
                            if (currentReview != null) { revOut.Add(currentReview); }
                        });
                    }
                    Task.WaitAll(revTasks);

                    Reviews.AddRange(revOut);
                }
            }
        }

        private Review processReview(HtmlNode current, string reviewNumber)
        {
            string Date = current.CssSelect("meta[itemProp='datePublished']").Single().GetAttributeValue("content").Trim();

            string Rating = current.CssSelect("div[itemProp='reviewRating'] span[itemProp='ratingValue']").Single().InnerText.Trim();
            Rating = Rating.Substring(1);
            Rating = Rating.Substring(0, Rating.Length - 1);

            string Review = current.CssSelect("p[itemProp='reviewBody']").Single().InnerText.Trim();
            Review = Review.Replace("  ", " ");
            Review = Review.Replace(Environment.NewLine, "");

            string Response;
            var ResponseNode = current.CssSelect(".restaurant-content span.responseexpandable").FirstOrDefault();
            if (ResponseNode != default(HtmlNode))
            {
                Response = ResponseNode.InnerText.Trim();
                Response = Response.Replace("  ", " ");
                Response = Response.Replace(Environment.NewLine, "");
            }
            else
            {
                Response = "NULL";
            }

            return new Review(reviewNumber, PrimaryKey, Date, Rating, Review, Response);
        }

        private static HtmlNode getDocumentNode(string URL)
        {
            HtmlNode docNode = null;
            using (var web = new CompressedWebClient())
            {
                bool tryAgain = true;
                string currentFile = null;
                while (tryAgain)
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

        public override string ToString()
        {
            return String.Format("{0},\"{1}\",\"{2}\",{3},{4},{5},\"{6}\",{7}", PrimaryKey, Name, Address, Score, NumberOfReviews, CuisineType, BestFor, AverageSpend);
        }

        #region Exceptions
        private static void throw404(string url)
        {
            throw new System.Web.HttpException(404, string.Format("Webpage did not respond [{0}]", url));
        }
        #endregion
    }
}
