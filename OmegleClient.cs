namespace OmegleSharp
{
    using LegitHttpClient;
    using Newtonsoft.Json.Linq;
    using System.Text;

    public class OmegleClient
    {
        private static string[] ServersList = new string[] { "front46", "front19", "front26", "front41", "front8", "front24", "front3", "front44", "front18", "front14", "front35", "front40", "front23", "front27", "front2", "front28", "front12", "front20", "front37", "front39", "front4", "front22", "front11", "front16", "front34", "front45", "front38", "front29", "front43", "front30", "front48", "front9", "front15", "front7", "front13", "front17", "front32", "front36", "front5", "front33", "front42", "front31", "front47", "front10", "front25", "front21", "front6" };
        private static ProtoRandom protoRandom = new ProtoRandom(1);

        public static OmegleSession ConnectToStranger(string[] interests = null, string proxyUrl = "", string proxyUsername = "", string proxyPassword = "")
        {
            try
            {
                string server = ServersList[protoRandom.GetRandomInt32(0, ServersList.Length - 1)];
                string randID = protoRandom.GetRandomString("23456789ABCDEFGHJKLMNPQRSTUVWXYZ".ToCharArray(), 8);

                HttpClient client = new HttpClient();
                client.ConnectTo(server + ".omegle.com", false, 80, proxyUrl, proxyUsername, proxyPassword);

                HttpRequest request = new HttpRequest();
                request.Method = HttpMethod.POST;
                request.Version = HttpVersion.HTTP_10;

                if (interests == null)
                {
                    request.URI = $"/start?caps=recaptcha2,t&firstevents=1&spid=&randid={randID}&lang=it";
                }
                else
                {
                    string interestsStr = "";

                    foreach (string interest in interests)
                    {
                        if (interestsStr == "")
                        {
                            interestsStr = "\"" + interest + "\"";
                        }
                        else
                        {
                            interestsStr += ",\"" + interest + "\"";
                        }
                    }

                    interestsStr = "[" + interestsStr + "]";
                    request.URI = $"/start?caps=recaptcha2,t&firstevents=1&spid=&randid={randID}&topics={interestsStr}&lang=it";
                }

                request.Headers.Add(new HttpHeader() { Name = "Host", Value = server + ".omegle.com" });

                string body = Encoding.UTF8.GetString(client.Send(request).Body);
                bool connected = body.Contains("[\"connected\"]");
                bool sameLanguage = body.Contains("language");
                dynamic jss = JObject.Parse(body);
                string clientID = jss.clientID;

                while (!connected)
                {
                    try
                    {
                        HttpClient client1 = new HttpClient();
                        client1.ConnectTo(server + ".omegle.com", false, 80, proxyUrl, proxyUsername, proxyPassword);

                        HttpRequest request1 = new HttpRequest();
                        request1.Method = HttpMethod.POST;
                        request1.Version = HttpVersion.HTTP_10;
                        request1.URI = $"/events";
                        request1.Body = Encoding.UTF8.GetBytes("id=" + clientID.Replace(":", "%3A"));

                        request1.Headers.Add(new HttpHeader() { Name = "Host", Value = server + ".omegle.com" });
                        request1.Headers.Add(new HttpHeader() { Name = "Content-type", Value = "application/x-www-form-urlencoded; charset=UTF-8" });
                        request1.Headers.Add(new HttpHeader() { Name = "Content-Length", Value = request1.Body.Length.ToString() });

                        string body1 = Encoding.UTF8.GetString(client1.Send(request1).Body);
                        connected = body1.Contains("[\"connected\"]");
                        sameLanguage = body1.Contains("language");
                    }
                    catch
                    {

                    }
                }

                return new OmegleSession()
                {
                    RandID = randID,
                    ClientID = clientID,
                    Connected = true,
                    Server = server,
                    ProxyURL = proxyUrl,
                    ProxyUsername = proxyUsername,
                    ProxyPassword = proxyPassword,
                    SameLanguage = sameLanguage
                };
            }
            catch
            {
                return null;
            }
        }

        public static string FixJSONString(string str)
        {
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == '[' || str[i] == '{')
                {
                    str = str.Substring(i);
                    break;
                }
            }

            int steps = 0;

            for (int i = str.Length - 1; i >= 0; i--)
            {
                if (str[i] == ']' || str[i] == '}')
                {
                    str = str.Substring(0, str.Length - steps);
                    break;
                }

                steps++;
            }

            return str;
        }
    }
}