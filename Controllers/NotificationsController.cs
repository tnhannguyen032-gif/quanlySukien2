using System;
using System.Linq;
using System.Web.Mvc;
using quanlySukien.Models;

namespace quanlySukien.Controllers
{
    public class NotificationsController : Controller
    {
        private event_managementEntities db = new event_managementEntities();

        private bool IsStudent()
        {
            return Session["UserId"] != null &&
                   Session["Role"] != null &&
                   Session["Role"].ToString() == "student";
        }

        // GET: Notifications
        public ActionResult Index()
        {
            if (!IsStudent())
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = Convert.ToInt32(Session["UserId"]);

            var notifications = db.notifications
                .Where(n => n.user_id == userId)
                .OrderByDescending(n => n.created_at)
                .ToList();

            return View(notifications);
        }

        // GET: Notifications/Read/5
        public ActionResult Read(int? id)
        {
            if (!IsStudent())
            {
                return RedirectToAction("Login", "Account");
            }

            if (id == null)
            {
                return new HttpStatusCodeResult(400);
            }

            int userId = Convert.ToInt32(Session["UserId"]);

            var notification = db.notifications.FirstOrDefault(n =>
                n.id == id &&
                n.user_id == userId);

            if (notification == null)
            {
                return HttpNotFound();
            }

            // Đánh dấu đã đọc
            notification.is_read = true;

            db.SaveChanges();

            return RedirectToAction("Index");
        }

        // Đánh dấu tất cả đã đọc
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarkAllAsRead()
        {
            if (!IsStudent())
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = Convert.ToInt32(Session["UserId"]);

            var notifications = db.notifications
                .Where(n => n.user_id == userId &&
                            !n.is_read)
                .ToList();

            foreach (var notification in notifications)
            {
                notification.is_read = true;
            }

            db.SaveChanges();

            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}