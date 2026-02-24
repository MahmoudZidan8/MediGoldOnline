
using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(GuardianOnline.Startup))]
namespace GuardianOnline
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
            app.MapSignalR();
        }
    }
}
