using quanlySukien.Models;
using System;
using System.Linq;
using System.Web.Mvc;

namespace quanlySukien.Controllers
{
    public class FeedbacksController : Controller
    {
        private event_managementEntities db = new event_managementEntities();

        private bool IsStudent()
        {
            return Session["UserId"] != null &&
                   Session["Role"] != null &&
                   Session["Role"].ToString() == "student";
        }

        // GET: Feedbacks/Create?eventId=1
        public ActionResult Create(int? eventId)
        {
            if (!IsStudent())
            {
                return RedirectToAction("Login", "Account");
            }

            if (eventId == null)
            {
                return new HttpStatusCodeResult(400);
            }

            int userId = Convert.ToInt32(Session["UserId"]);

            var eventItem = db.events
                .FirstOrDefault(e => e.id == eventId &&
                                     e.status == "approved");

            if (eventItem == null)
            {
                return HttpNotFound();
            }

            // Kiểm tra sinh viên đã đăng ký và được duyệt
            var registration = db.registrations.FirstOrDefault(r =>
                r.user_id == userId &&
                r.event_id == eventId &&
                r.status == "approved");

            if (registration == null)
            {
                TempData["Warning"] =
                    "Bạn chưa được duyệt tham gia sự kiện này.";

                return RedirectToAction("Details", "Events",
                    new { id = eventId });
            }

            // Kiểm tra đã đánh giá chưa
            var existedFeedback = db.feedbacks.FirstOrDefault(f =>
                f.user_id == userId &&
                f.event_id == eventId);

            if (existedFeedback != null)
            {
                TempData["Warning"] =
                    "Bạn đã đánh giá sự kiện này rồi.";

                return RedirectToAction("Details", "Events",
                    new { id = eventId });
            }

            ViewBag.EventId = eventItem.id;
            ViewBag.EventTitle = eventItem.title;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(int eventId, int rating, string comment)
        {
            if (!IsStudent())
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = Convert.ToInt32(Session["UserId"]);

            var eventItem = db.events.FirstOrDefault(e =>
                e.id == eventId &&
                e.status == "approved");

            if (eventItem == null)
            {
                return HttpNotFound();
            }

            // Kiểm tra đăng ký đã được duyệt
            var registration = db.registrations.FirstOrDefault(r =>
                r.user_id == userId &&
                r.event_id == eventId &&
                r.status == "approved");

            if (registration == null)
            {
                TempData["Warning"] =
                    "Bạn chưa được duyệt tham gia sự kiện này.";

                return RedirectToAction("Details", "Events",
                    new { id = eventId });
            }

            // Kiểm tra đã đánh giá
            var existedFeedback = db.feedbacks.FirstOrDefault(f =>
                f.user_id == userId &&
                f.event_id == eventId);

            if (existedFeedback != null)
            {
                TempData["Warning"] =
                    "Bạn đã đánh giá sự kiện này rồi.";

                return RedirectToAction("Details", "Events",
                    new { id = eventId });
            }

            // Kiểm tra số sao
            if (rating < 1 || rating > 5)
            {
                TempData["Warning"] =
                    "Số sao phải từ 1 đến 5.";

                return RedirectToAction("Create",
                    new { eventId = eventId });
            }

            var feedback = new feedback
            {
                user_id = userId,
                event_id = eventId,
                rating = rating,
                comment = comment,
                created_at = DateTime.Now
            };

            db.feedbacks.Add(feedback);
            db.SaveChanges();

            TempData["Success"] =
                "Đánh giá sự kiện thành công.";

            return RedirectToAction("Details", "Events",
                new { id = eventId });
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