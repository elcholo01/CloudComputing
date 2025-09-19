using System.Web.Mvc;

namespace MovieDiscussionService.Controllers
{
    public class HealthController : Controller
    {
        [HttpGet]
        [Route("health-monitoring")]
        public ActionResult HealthMonitoring()
        {
            // Jednostavan health check endpoint
            // Vraća OK status ako servis radi
            return new HttpStatusCodeResult(200, "OK - MovieDiscussionService is healthy");
        }
    }
}