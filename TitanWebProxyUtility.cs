using System.Net;
using System.Text.RegularExpressions;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Models;

namespace PockerPicker
{
    public class TitanWebProxyUtility
    {
        const string tmp = "--------------------------------------------";
        private Dictionary<string, string> Rules { get; set; }

        public ProxyServer proxyServer = new();

        public ExplicitProxyEndPoint explicitEndPoint = new ExplicitProxyEndPoint(IPAddress.Any, 8000, true)
        {
            // Use self-issued generic certificate on all https requests
            // Optimizes performance by not creating a certificate for each https-enabled domain
            // Useful when certificate trust is not required by proxy clients
            //GenericCertificate = new X509Certificate2(Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "genericcert.pfx"), "password")
        };

        // Transparent endpoint is useful for reverse proxy (client is not aware of the existence of proxy)
        // A transparent endpoint usually requires a network router port forwarding HTTP(S) packets or DNS
        // to send data to this endPoint
        public TransparentProxyEndPoint transparentEndPoint = new TransparentProxyEndPoint(IPAddress.Any, 8001, true)
        {
            // Generic Certificate hostname to use
            // when SNI is disabled by client
            GenericCertificateName = "google.com"
        };

        public TitanWebProxyUtility(Dictionary<string, string> rules)
        {
            Rules = rules;

            // locally trust root certificate used by this proxy 
            proxyServer.CertificateManager.CreateRootCertificate(true);
            proxyServer.CertificateManager.TrustRootCertificate(true);

            // optionally set the Certificate Engine
            // Under Mono only BouncyCastle will be supported
            //proxyServer.CertificateManager.CertificateEngine = Network.CertificateEngine.BouncyCastle;

            proxyServer.BeforeRequest += OnRequest;
            proxyServer.BeforeResponse += OnResponse;
            proxyServer.ServerCertificateValidationCallback += OnCertificateValidation;
            proxyServer.ClientCertificateSelectionCallback += OnCertificateSelection;

            // Fired when a CONNECT request is received
            explicitEndPoint.BeforeTunnelConnectRequest += OnBeforeTunnelConnectRequest;

            // An explicit endpoint is where the client knows about the existence of a proxy
            // So client sends request in a proxy friendly manner
            proxyServer.AddEndPoint(explicitEndPoint);

            proxyServer.AddEndPoint(transparentEndPoint);

            //proxyServer.UpStreamHttpProxy = new ExternalProxy() { HostName = "localhost", Port = 8888 };
            //proxyServer.UpStreamHttpsProxy = new ExternalProxy() { HostName = "localhost", Port = 8888 };

            foreach (var endPoint in proxyServer.ProxyEndPoints)
                Console.WriteLine("Listening on '{0}' endpoint at Ip {1} and port: {2} ",
                    endPoint.GetType().Name, endPoint.IpAddress, endPoint.Port);
        }

        public void ProxyStart()
        {
            proxyServer.Start();
            // Only explicit proxies can be set as system proxy!
            proxyServer.SetAsSystemHttpProxy(explicitEndPoint);
            proxyServer.SetAsSystemHttpsProxy(explicitEndPoint);
        }

        public void ProxyStop()
        {
            // Unsubscribe & Quit
            explicitEndPoint.BeforeTunnelConnectRequest -= OnBeforeTunnelConnectRequest;
            proxyServer.BeforeRequest -= OnRequest;
            proxyServer.BeforeResponse -= OnResponse;
            proxyServer.ServerCertificateValidationCallback -= OnCertificateValidation;
            proxyServer.ClientCertificateSelectionCallback -= OnCertificateSelection;

            proxyServer.Stop();
        }

        private async Task OnBeforeTunnelConnectRequest(object sender, TunnelConnectSessionEventArgs e)
        {
        }

        public async Task OnRequest(object sender, SessionEventArgs e)
        {
        }

        
        public async Task OnResponse(object sender, SessionEventArgs e)
        {
            // read response headers
            var responseHeaders = e.HttpClient.Response.Headers;
            var requestUri = e.HttpClient.Request.RequestUriString;

            if (e.HttpClient.Request.Method == "GET" || e.HttpClient.Request.Method == "POST")
            {
                if (e.HttpClient.Response.StatusCode == 200)
                {
                    string[] ruleValues = GetRules(requestUri);
                    //if (e.HttpClient.Response.ContentType != null && e.HttpClient.Response.ContentType.Trim().ToLower().Contains("text/html"))
                    if (e.HttpClient.Response.ContentType != null && (ruleValues.Length != 0))
                    {
                        //e.SetResponseBody(bodyBytes);

                        string body = await e.GetResponseBodyAsString();
                        //e.SetResponseBodyString(body);
                        SaveFile.SaveTo(ruleValues, body);
                        Console.WriteLine($"Request : {Environment.NewLine} {requestUri}");
                        Console.WriteLine(tmp);
                    }
                }
            }
        }

        // Allows overriding default certificate validation logic
        public Task OnCertificateValidation(object sender, CertificateValidationEventArgs e)
        {
            // set IsValid to true/false based on Certificate Errors
            if (e.SslPolicyErrors == System.Net.Security.SslPolicyErrors.None)
                e.IsValid = true;

            return Task.CompletedTask;
        }

        // Allows overriding default client certificate selection logic during mutual authentication
        public Task OnCertificateSelection(object sender, CertificateSelectionEventArgs e)
        {
            // set e.clientCertificate to override
            return Task.CompletedTask;
        }

        private bool InRule(string uri)
        {
            bool res = false;
            foreach(string key in Rules.Keys)
            {
                if (uri.Contains(key))
                {
                    res = true;
                    break;
                }
            }
            return res;
        }

        private string GetRule(string uri)
        {
            string res = String.Empty;
            foreach(string key in Rules.Keys)
            { 
                if (Regex.IsMatch(uri, key))
                {
                    res = Rules[key];
                    break;
                }
            }
            return res;
        }

        private string[] GetRules(string uri)
        {
            List<string> res = new();
            foreach (string key in Rules.Keys)
            {
                if (Regex.IsMatch(uri, key))
                {
                    res.Add(Rules[key]);
                }
            }
            return res.ToArray();
        }
    }
}
