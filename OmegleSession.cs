namespace OmegleSharp
{
    using System.Threading;
    using LegitHttpClient;
    using System.Text;
    using Newtonsoft.Json;
    using System;

    public class OmegleSession
    {
        public string Server { get; set; }
        public string RandID { get; set; }
        public string ClientID { get; set; }
        public bool Connected { get; set; }
        public string ProxyURL { get; set; }
        public string ProxyUsername { get; set; }
        public string ProxyPassword { get; set; }
        public bool SameLanguage { get; set; }
        private Thread FetchThread { get; set; }

        public event EventHandler<OmegleTypingEventArgs> OnTyping;
        public event EventHandler<OmegleMessageEventArgs> OnMessage;
        public event EventHandler<OmegleDisconnectEventArgs> OnDisconnect;

        public void Start()
        {
            FetchThread = new Thread(FetchEvents);
            FetchThread.Priority = ThreadPriority.Highest;
            FetchThread.Start();
        }

        protected virtual void FireTyping(OmegleTypingEventArgs e)
        {
            EventHandler<OmegleTypingEventArgs> handler = OnTyping;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void FireMessage(OmegleMessageEventArgs e)
        {
            EventHandler<OmegleMessageEventArgs> handler = OnMessage;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void FireDisconnect(OmegleDisconnectEventArgs e)
        {
            EventHandler<OmegleDisconnectEventArgs> handler = OnDisconnect;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        public void Stop()
        {
            try
            {
                FetchThread.Suspend();
            }
            catch
            {

            }

            try
            {
                FetchThread.Interrupt();
            }
            catch
            {

            }

            try
            {
                FetchThread.Abort();
            }
            catch
            {

            }
        }

        public void SendMessage(string message)
        {
            try
            {
                HttpClient client = new HttpClient();
                client.ConnectTo(Server + ".omegle.com", false, 80, ProxyURL, ProxyUsername, ProxyPassword);

                HttpRequest request = new HttpRequest();
                request.Method = HttpMethod.POST;
                request.Version = HttpVersion.HTTP_10;
                request.URI = $"/send";
                request.Body = Encoding.UTF8.GetBytes("msg=" + System.Web.HttpUtility.UrlEncode(message) + "&id=" + ClientID.Replace(":", "%3A"));

                request.Headers.Add(new HttpHeader() { Name = "Host", Value = Server + ".omegle.com" });
                request.Headers.Add(new HttpHeader() { Name = "Content-type", Value = "application/x-www-form-urlencoded; charset=UTF-8" });
                request.Headers.Add(new HttpHeader() { Name = "Content-Length", Value = request.Body.Length.ToString() });

                client.Send(request);
                client.Disconnect();
            }
            catch
            {

            }
        }

        public void SendTyping()
        {
            try
            {
                HttpClient client = new HttpClient();
                client.ConnectTo(Server + ".omegle.com", false, 80, ProxyURL, ProxyUsername, ProxyPassword);

                HttpRequest request = new HttpRequest();
                request.Method = HttpMethod.POST;
                request.Version = HttpVersion.HTTP_10;
                request.URI = $"/typing";
                request.Body = Encoding.UTF8.GetBytes("id=" + ClientID.Replace(":", "%3A"));

                request.Headers.Add(new HttpHeader() { Name = "Host", Value = Server + ".omegle.com" });
                request.Headers.Add(new HttpHeader() { Name = "Content-type", Value = "application/x-www-form-urlencoded; charset=UTF-8" });
                request.Headers.Add(new HttpHeader() { Name = "Content-Length", Value = request.Body.Length.ToString() });

                client.Send(request);
                client.Disconnect();
            }
            catch
            {

            }
        }

        public void Disconnect()
        {
            try
            {
                Stop();

                if (Connected)
                {
                    Connected = false;

                    HttpClient client = new HttpClient();
                    client.ConnectTo(Server + ".omegle.com", false, 80, ProxyURL, ProxyUsername, ProxyPassword);

                    HttpRequest request = new HttpRequest();
                    request.Method = HttpMethod.POST;
                    request.Version = HttpVersion.HTTP_10;
                    request.URI = $"/disconnect";
                    request.Body = Encoding.UTF8.GetBytes("id=" + ClientID.Replace(":", "%3A"));

                    request.Headers.Add(new HttpHeader() { Name = "Host", Value = Server + ".omegle.com" });
                    request.Headers.Add(new HttpHeader() { Name = "Content-type", Value = "application/x-www-form-urlencoded; charset=UTF-8" });
                    request.Headers.Add(new HttpHeader() { Name = "Content-Length", Value = request.Body.Length.ToString() });

                    client.Send(request);
                    client.Disconnect();
                }
            }
            catch
            {

            }
        }

        private void FetchEvents()
        {
            while (Connected)
            {
                try
                {
                    HttpClient client = new HttpClient();
                    client.ConnectTo(Server + ".omegle.com", false, 80, ProxyURL, ProxyUsername, ProxyPassword);

                    HttpRequest request = new HttpRequest();
                    request.Method = HttpMethod.POST;
                    request.Version = HttpVersion.HTTP_10;
                    request.URI = $"/events";
                    request.Body = Encoding.UTF8.GetBytes("id=" + ClientID.Replace(":", "%3A"));

                    request.Headers.Add(new HttpHeader() { Name = "Host", Value = Server + ".omegle.com" });
                    request.Headers.Add(new HttpHeader() { Name = "Content-Length", Value = request.Body.Length.ToString() });
                    request.Headers.Add(new HttpHeader() { Name = "Content-type", Value = "application/x-www-form-urlencoded; charset=UTF-8" });

                    string body = OmegleClient.FixJSONString(Encoding.UTF8.GetString(client.Send(request).Body));
                    client.Disconnect();

                    if (!body.Contains("[") && !body.Contains("]") && !body.Contains("\""))
                    {
                        continue;
                    }

                    dynamic jss = JsonConvert.DeserializeObject(body);

                    foreach (var item in jss)
                    {
                        string eventType = item[0];

                        if (eventType == "typing")
                        {
                            FireTyping(new OmegleTypingEventArgs());
                        }
                        else if (eventType == "gotMessage")
                        {
                            FireMessage(new OmegleMessageEventArgs() { Message = item[1] });
                        }
                        else if (eventType == "strangerDisconnected")
                        {
                            FireDisconnect(new OmegleDisconnectEventArgs());
                            Connected = false;
                            return;
                        }
                    }
                }
                catch
                {

                }
            }
        }
    }
}