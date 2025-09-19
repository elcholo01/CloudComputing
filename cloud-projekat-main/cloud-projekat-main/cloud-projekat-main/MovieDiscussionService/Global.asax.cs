using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace MovieDiscussionService
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            // Minimal MVC setup for guaranteed functionality
            RouteTable.Routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}