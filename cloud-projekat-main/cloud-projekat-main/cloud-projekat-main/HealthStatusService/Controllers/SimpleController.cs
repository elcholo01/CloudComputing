using System;
using System.Web.Mvc;

namespace HealthStatusService.Controllers
{
    public class SimpleController : Controller
    {
        // GET: Simple
        public ActionResult Index()
        {
            ViewBag.Title = "Health Status Service - RADI!";
            ViewBag.Message = "Web aplikacija je uspešno pokrenuta!";
            ViewBag.CurrentTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");

            return View();
        }

        public ActionResult Test()
        {
            var testData = new
            {
                Status = "OK",
                Message = "HealthStatusService API Test uspešan!",
                Timestamp = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"),
                ServerName = Environment.MachineName,
                ApplicationVersion = "1.0.0"
            };

            var jsonResponse = $@"{{
    ""Status"": ""{testData.Status}"",
    ""Message"": ""{testData.Message}"",
    ""Timestamp"": ""{testData.Timestamp}"",
    ""ServerName"": ""{testData.ServerName}"",
    ""ApplicationVersion"": ""{testData.ApplicationVersion}""
}}";

            return Content(jsonResponse, "application/json");
        }
    }
}