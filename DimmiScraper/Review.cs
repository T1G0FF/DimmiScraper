using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DimmiScraper
{
    [Serializable]
    class Review
    {
        public string PrimaryKey;
        public string Restaurant;
        public string DatePosted;
        public string Rating;
        public string ReviewText;
        public string Response;

        public Review(string primaryKey, string restaurant, string date, string rating, string reviewText, string response)
        {
            PrimaryKey = primaryKey;
            Restaurant = restaurant;
            DatePosted = date;
            Rating = rating;
            ReviewText = reviewText;
            Response = response;
        }

        public override string ToString()
        {
            return String.Format("{0},{1},{2},{3},\"{4}\",\"{5}\"", PrimaryKey, Restaurant, DatePosted, Rating, ReviewText, Response);
        }
    }
}
