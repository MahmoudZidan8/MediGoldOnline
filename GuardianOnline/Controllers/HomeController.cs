using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace GuardianOnline.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            if ((Session["UserID"] == null) || (int.Parse(Session["IsAdmin"].ToString()) == 0))
            {
                return RedirectToAction("login", "ClaimForm");
            }
            return View();
        }

        public ActionResult About()
        {
            if ((Session["UserID"] == null) || (int.Parse(Session["IsAdmin"].ToString()) == 0))
            {
                return RedirectToAction("login", "ClaimForm");
            }
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            //if ((Session["UserID"] == null) || (int.Parse(Session["IsAdmin"].ToString()) == 0))
            //{
            //    return RedirectToAction("login", "ClaimForm");
            //}
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}