using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using GuardianOnline.App_Start; // ADD THIS

namespace GuardianOnline
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            SqlDependency.Start(ConfigurationManager.ConnectionStrings["CustomerConnection"].ConnectionString);

        }
        protected void Application_End()
        {
            SqlDependency.Stop(ConfigurationManager.ConnectionStrings["CustomerConnection"].ConnectionString);
        }

        // ADD THIS METHOD - Executes on EVERY request
        protected void Application_BeginRequest()
        {
            LocalizationConfig.ApplyCulture(
                new HttpRequestWrapper(Request),
                new HttpResponseWrapper(Response)
            );
        }
    }
}
