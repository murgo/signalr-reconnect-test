using Microsoft.AspNet.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace SignalRReconnectTest
{
    public static class Program
    {
        public const string Url = "http://localhost:56200/";

        public static void Main(string[] args)
        {
            Way3();
        }

        private static void Way3()
        {
            var key1 = "010101-0101";
            var key2 = "010101-0102";
            var key3 = "010101-0103";

            var connector = new Connector(Url, "Subscriber", "Publisher");
            var subscriber = new SubClient(connector);

            subscriber.SubscribeAsync(key1, m => Console.WriteLine("Received1: {0}", m));
            subscriber.SubscribeAsync(key2, m => Console.WriteLine("Received2: {0}", m));
            subscriber.SubscribeAsync(key3, m => Console.WriteLine("Received3: {0}", m));

            var publisher = new PubClient(connector);

            var timer = new System.Timers.Timer(3000) { AutoReset = true };
            timer.Elapsed += (s, a) =>
            {
                Console.WriteLine("Publishing message...");
                publisher.PublishAsync(key1);
                Thread.Sleep(1000);
                publisher.PublishAsync(key2);
                Thread.Sleep(1000);
                publisher.PublishAsync(key3);
            };
            timer.Start();

            Console.ReadLine();
        }

        private static void Way2()
        {
            var key = "010101-0101";

            var sconn = new Connector(Url, "Subscriber");
            var subscriber = new SubClient(sconn);

            subscriber.SubscribeAsync(key, m => Console.WriteLine("Received: {0}", m));

            var pconn = new Connector(Url, "Publisher");
            var publisher = new PubClient(pconn);

            var timer = new System.Timers.Timer(1000) { AutoReset = true };
            int i = 0;
            timer.Elapsed += (s, a) =>
            {
                Console.WriteLine("Publishing message...");
                publisher.PublishAsync(key);
            };
            timer.Start();

            Console.ReadLine();
        }

        private static void Way1()
        {
            var key = "010101-0101";

            Console.WriteLine("Establishing connection 1...");
            var pubConnection = new HubConnection(Url) { TraceLevel = TraceLevels.All, TraceWriter = Console.Out };
            //pubConnection.Closed += () => pubConnection.Start(); // HOX
            var publisher = pubConnection.CreateHubProxy("Publisher");
            pubConnection.Start().Wait();

            Console.WriteLine("Establishing connection 2...");
            var subConnection = new HubConnection(Url) { TraceLevel = TraceLevels.All, TraceWriter = Console.Out };
            var subscriber = subConnection.CreateHubProxy("Subscriber");
            subConnection.Closed += () => subConnection.Start().ContinueWith(r => subscriber.Invoke<string>("Subscribe", key), TaskContinuationOptions.NotOnFaulted); // HOX
            subConnection.Start().ContinueWith(r => subscriber.Invoke<string>("Subscribe", key)).Wait();

            Console.WriteLine("Subscribing to events...");
            subscriber.On<string>("Message", s => Console.WriteLine("Got message: {0}", s));

            var timer = new System.Timers.Timer(1000) { AutoReset = true };
            timer.Elapsed += (s, a) => {
                Console.WriteLine("Publishing message...");
                publisher.Invoke("Publish", key);
            };
            timer.Start();

            Console.ReadLine();
        }
    }
}
