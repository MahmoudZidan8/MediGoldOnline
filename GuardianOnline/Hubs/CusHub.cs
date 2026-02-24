using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace Customer.Hubs
{
    public class CusHub : Hub
    {
        public static async Task Show1()
        {
            IHubContext context = GlobalHost.ConnectionManager.GetHubContext<CusHub>();
            await context.Clients.All.displayCustomer();
        }
        public static async Task Show()
        {
            IHubContext context = GlobalHost.ConnectionManager.GetHubContext<CusHub>();
            await context.Clients.All.data();
        }
        public static async Task Show2()
        {
            IHubContext context = GlobalHost.ConnectionManager.GetHubContext<CusHub>();
            await context.Clients.All.data1();
        }
    }
    //public class CusHub1 : Hub
    //{
    //    public static void Show()
    //    {
    //        IHubContext context = GlobalHost.ConnectionManager.GetHubContext<CusHub>();
    //        context.Clients.All.data();
    //    }

    //}
}