using quanlySukien;
using quanlySukien.Models;
using System;
using System.Linq;
using System.Web.Mvc;

namespace quanlySukien.Controllers
{
    public class RegistrationsController : Controller
    {
        private event_managementEntities db = new event_managementEntities();

        // GET: Registrations/Create?eventId=1
        public ActionResult Create(int? eventId)
        {
            // Chưa đăng nhập
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (eventId == null)
            {
                return new HttpStatusCodeResult(400);
            }

            var eventItem = db.events
                .FirstOrDefault(e => e.id == eventId);

            if (eventItem == null)
            {
                return HttpNotFound();
            }

            return View(eventItem);
        }

        // POST: Registrations/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(int eventId)
        {
            // Kiểm tra đăng nhập
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = Convert.ToInt32(Session["UserId"]);

            // Kiểm tra sự kiện
            var eventItem = db.events
                .FirstOrDefault(e => e.id == eventId);

            if (eventItem == null)
            {
                return HttpNotFound();
            }

            // Chỉ cho đăng ký sự kiện đã được duyệt
            if (eventItem.status != "approved")
            {
                TempData["Error"] =
                    "Sự kiện này chưa được mở đăng ký.";

                return RedirectToAction(
                    "Details",
                    "Events",
                    new { id = eventId }
                );
            }

            // Kiểm tra hạn đăng ký
            if (eventItem.registration_deadline.HasValue &&
                DateTime.Now > eventItem.registration_deadline.Value)
            {
                TempData["Error"] =
                    "Đã hết hạn đăng ký sự kiện.";

                return RedirectToAction(
                    "Details",
                    "Events",
                    new { id = eventId }
                );
            }

            // Kiểm tra sinh viên đã đăng ký chưa
            var existingRegistration = db.registrations
                .FirstOrDefault(r =>
                    r.user_id == userId &&
                    r.event_id == eventId);

            if (existingRegistration != null)
            {
                TempData["Error"] =
                    "Bạn đã đăng ký sự kiện này rồi.";

                return RedirectToAction(
                    "Details",
                    "Events",
                    new { id = eventId }
                );
            }

            // Kiểm tra số lượng người tham gia
            if (eventItem.max_participants.HasValue)
            {
                int currentApproved = db.registrations
                    .Count(r =>
                        r.event_id == eventId &&
                        r.status == "approved");

                if (currentApproved >= eventItem.max_participants.Value)
                {
                    TempData["Error"] =
                        "Sự kiện đã đủ số lượng người tham gia.";

                    return RedirectToAction(
                        "Details",
                        "Events",
                        new { id = eventId }
                    );
                }
            }

            // Tạo đăng ký
            var registration = new registration
            {
                user_id = userId,
                event_id = eventId,
                status = "pending",
                registered_at = DateTime.Now
            };

            db.registrations.Add(registration);
            db.SaveChanges();

            TempData["Success"] =
                "Đăng ký tham gia thành công. Vui lòng chờ kiểm duyệt.";

            return RedirectToAction(
                "Details",
                "Events",
                new { id = eventId }
            );
        }

        // GET: Registrations/MyEvents
        public ActionResult MyEvents()
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = Convert.ToInt32(Session["UserId"]);

            // Lấy các đăng ký của sinh viên
            var registrations = db.registrations
                .Where(r => r.user_id == userId)
                .OrderByDescending(r => r.registered_at)
                .ToList();

            // ==============================
            // TÊN SỰ KIỆN
            // ==============================

            var eventIds = registrations
                .Select(r => r.event_id)
                .Distinct()
                .ToList();

            var eventTitles = db.events
                .Where(e => eventIds.Contains(e.id))
                .ToDictionary(e => e.id, e => e.title);

            ViewBag.EventTitles = eventTitles;


            // ==============================
            // TRẠNG THÁI ĐIỂM DANH
            // ==============================

            var registrationIds = registrations
                .Select(r => r.id)
                .ToList();

            var attendanceList = db.attendances
                .Where(a => registrationIds.Contains(a.registration_id)).ToList();

            var attendanceStatuses = attendanceList
                .ToDictionary(a => a.registration_id, a => a.status);

            ViewBag.AttendanceStatuses = attendanceStatuses;


            // ==============================
            // TRẠNG THÁI ĐÁNH GIÁ
            // ==============================

            var feedbackList = db.feedbacks
                .Where(f => f.user_id == userId &&
                            eventIds.Contains(f.event_id))
                .ToList();

            var feedbackEventIds = feedbackList
                .Select(f => f.event_id)
                .Distinct()
                .ToList();

            ViewBag.FeedbackEventIds = feedbackEventIds;


            return View(registrations);
        }
    }
}