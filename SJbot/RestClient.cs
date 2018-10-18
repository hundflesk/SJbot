namespace SJbot
{
    public class RestClient
    {
        public string Url { get; set; }
        public HttpVerb HttpMethod { get; set; }
        public string UserName { get; set; }
        public string UserPassword { get; set; }

        public RestClient()
        {
            Url = "http://api.tagtider.net/v1/stations/243/transfers/departures.json";
            HttpMethod = HttpVerb.GET;
            UserName = "tagtider";
            UserPassword = "codemocracy";
        }
    }

    public enum HttpVerb { GET }
}
