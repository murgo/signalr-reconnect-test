using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SignalRReconnectTest
{
    public class Connector
    {
        private HubConnection _hubConnection;
        private readonly Dictionary<string, IHubProxy> _proxies = new Dictionary<string, IHubProxy>();
        public event EventHandler Reconnected;

        public Connector(string url, params string[] proxies)
        {
            _hubConnection = new HubConnection(url) { TraceLevel = TraceLevels.All, TraceWriter = Console.Out };
            _hubConnection.Closed += HubConnectionClosed;

            foreach (string p in proxies)
            {
                _proxies.Add(p, _hubConnection.CreateHubProxy(p));
            }
        }

        private Task StartAsync()
        {
            //if (_hubConnection.State != ConnectionState.Disconnected) return Task.Factory.StartNew(() => { });

            return _hubConnection.Start();
        }

        private void HubConnectionClosed()
        {
            StartAsync().ContinueWith(e =>
            {
                if (Reconnected != null) Reconnected(this, null);
            }, TaskContinuationOptions.NotOnFaulted);
        }

        public IHubProxy GetProxy(string name)
        {
            return _proxies[name];
        }

        public Task RunAsync(Action<Task> t)
        {
            return StartAsync().ContinueWith(t, TaskContinuationOptions.NotOnFaulted);
        }
    }

    public class PubClient
    {
        private Connector _connector;

        public PubClient(Connector c)
        {
            _connector = c;
        }

        public Task PublishAsync(string id)
        {
            var proxy = _connector.GetProxy("Publisher");
            return _connector.RunAsync(t => proxy.Invoke("Publish", id));
        }
    }

    public class SubClient
    {
        private Connector _connector;
        private HashSet<string> _ids = new HashSet<string>();

        public SubClient(Connector c)
        {
            _connector = c;
            _connector.Reconnected += Reconnected;
        }

        private void Reconnected(object sender, EventArgs eventArgs)
        {
            var proxy = _connector.GetProxy("Subscriber");
            foreach (string id in _ids)
            {
                proxy.Invoke<string>("Subscribe", id);
            }
        }

        public Task SubscribeAsync(string id, Action<string> onMessage)
        {
            _ids.Add(id);

            var proxy = _connector.GetProxy("Subscriber");
            proxy.On<string>("Message", s => { if (s == id) onMessage(id); });

            return _connector.RunAsync(t => proxy.Invoke<string>("Subscribe", id));
        }
    }
}
