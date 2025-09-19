using System.Web.Mvc;

namespace MovieDiscussionService.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Title = "Movie Discussion Forum";
            ViewBag.Message = "Dobrodošli u Movie Discussion Forum!";
            ViewBag.Description = "Ovde možete diskutovati o filmovima, ocenjivati ih i deliti mišljenja.";
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Title = "O nama";
            ViewBag.Message = "Movie Discussion Forum - Sistem za diskusije o filmovima";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Title = "Kontakt";
            ViewBag.Message = "Kontakt informacije";
            return View();
        }
    }
}