using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using System.Threading.Tasks;

namespace Server
{
    public class Publisher : Hub
    {
        // HOX
        public Task Publish(string group)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<Subscriber>();
            // HOX (ettei oo othersingroup)
            return context.Clients.Group(group).Message(group);
        }
    }

    public class Subscriber : Hub
    {
        public Task Subscribe(string group)
        {
            return Groups.Add(Context.ConnectionId, group);
        }
    }
}