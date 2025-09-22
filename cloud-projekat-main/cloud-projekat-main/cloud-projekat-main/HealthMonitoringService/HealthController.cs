using System.Web.Mvc;

namespace HealthMonitoringService
{
    public class HealthController : Controller
    {
        [HttpGet]
        [Route("health-monitoring")]
        public ActionResult HealthMonitoring()
        {
            // Jednostavan health check endpoint
            // Vraća OK status ako servis radi
            return new HttpStatusCodeResult(200, "OK - HealthMonitoringService is healthy");
        }
    }
}
