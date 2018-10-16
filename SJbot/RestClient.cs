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
            Url = null;
            HttpMethod = HttpVerb.GET;
        }
    }

    public enum HttpVerb
    {
        GET,
        POST,
        PUT,
        DELETE
    }
}
