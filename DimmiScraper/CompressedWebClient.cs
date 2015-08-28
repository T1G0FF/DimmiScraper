using System;
using System.Net;

namespace DimmiScraper
{
    public class CompressedWebClient : WebClient
    {
        private HttpWebRequest _Request;
        
        protected override WebRequest GetWebRequest(Uri address)
        {
            _Request = base.GetWebRequest(address) as HttpWebRequest;
            _Request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            return _Request;
        }

        public HttpStatusCode StatusCode()
        {
            HttpStatusCode result;

            if (this._Request == null) { throw (new InvalidOperationException("Unable to retrieve the status code.")); }

            HttpWebResponse response = base.GetWebResponse(this._Request) as HttpWebResponse;
            if (response != null) { result = response.StatusCode; }
            else { throw new InvalidOperationException("Unable to retrieve the status code."); }

            return result;
        }
    }
}
