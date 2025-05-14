using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace WebServer
{
    public class Server
    {
        private static HttpListener listener;
        public static int maxSimultaneousconnections = 20;
        private static Semaphore sem = new Semaphore(maxSimultaneousconnections, maxSimultaneousconnections);


        private static List<IPAddress> GetLocalHostIPs()
        {
            IPHostEntry host;
            host = Dns.GetHostEntry(Dns.GetHostName());
            List<IPAddress> ret = host.AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork).ToList();
            return ret;
        }

        private static HttpListener InitializeListener(List<IPAddress> localhostIPs) 
        {
            HttpListener listener = new();
            listener.Prefixes.Add("http://localhost/");

            //Listen to IP address as well

            localhostIPs.ForEach(ip =>
            {
                Console.WriteLine("Listening on IP " + "http://" + ip.ToString() + "/");
                listener.Prefixes.Add("http://" + ip.ToString() + "/");

            });

            return listener;
        }

        private static void Start(HttpListener listener)
        {
            listener.Start();
            Task.Run(() => RunServer(listener));
        }  

        private static void RunServer(HttpListener listener)
        {
            while(true)
            {
                sem.WaitOne();
                StartConnectionListener(listener);

            }
        }

        private static async void StartConnectionListener(HttpListener listener)
        {
            //Wait for a connection. Return a caller while waiting.
            HttpListenerContext context = await listener.GetContextAsync();

            //Release the semaphore so that another listener can start up.
            sem.Release();
            Log(context.Request);

            //With connection do stuff...
            string response = "<html><head><meta http-equiv='content-type' content='text/html; charset=utf-8'/></head>Hello Browser!</html>";
            byte[] encoded = Encoding.UTF8.GetBytes(response);
            context.Response.ContentLength64 = encoded.Length;
            context.Response.OutputStream.Write(encoded, 0, encoded.Length);
            context.Response.OutputStream.Close();
        }

        public static void Start()
        {
            List<IPAddress> localHostIPs = GetLocalHostIPs();
            HttpListener listener = InitializeListener(localHostIPs);
            Start(listener);
        }

        public static void Log(HttpListenerRequest req)
        {
            Console.WriteLine(req.RemoteEndPoint + " " + req.HttpMethod + " /" + req.Url.AbsoluteUri);
        }

        
    }
}
