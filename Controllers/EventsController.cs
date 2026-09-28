using System;
using quanlySukien;
using quanlySukien.Models;
using System.Linq;
using System.Web.Mvc;

namespace quanlySukien.Controllers
{
    public class EventsController : Controller
    {
        private event_managementEntities db = new event_managementEntities();

        // GET: Events
        public ActionResult Index(
    string searchString,
    string filter,
    int? categoryId,
    DateTime? fromDate,
    DateTime? toDate)
        {
            var events = db.events.AsQueryable();

            // ==============================
            // CHỈ HIỂN THỊ SỰ KIỆN ĐÃ DUYỆT
            // ==============================

            events = events.Where(e =>
                e.status == "approved");


            // ==============================
            // TÌM KIẾM THEO TÊN / ĐỊA ĐIỂM
            // ==============================

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                events = events.Where(e =>
                    e.title.Contains(searchString) ||
                    e.location.Contains(searchString));
            }


            // ==============================
            // LỌC THEO DANH MỤC
            // ==============================

            if (categoryId.HasValue)
            {
                events = events.Where(e =>
                    e.category_id == categoryId.Value);
            }


            // ==============================
            // LỌC THEO TRẠNG THÁI THỜI GIAN
            // ==============================

            if (filter == "upcoming")
            {
                events = events.Where(e =>
                    e.start_time >= DateTime.Now);
            }
            else if (filter == "ongoing")
            {
                events = events.Where(e =>
                    e.start_time <= DateTime.Now &&
                    e.end_time >= DateTime.Now);
            }
            else if (filter == "past")
            {
                events = events.Where(e =>
                    e.end_time < DateTime.Now);
            }


            // ==============================
            // LỌC TỪ NGÀY
            // ==============================

            if (fromDate.HasValue)
            {
                DateTime startDate = fromDate.Value.Date;

                events = events.Where(e =>
                    e.start_time >= startDate);
            }


            // ==============================
            // LỌC ĐẾN NGÀY
            // ==============================

            if (toDate.HasValue)
            {
                DateTime endDate =
                    toDate.Value.Date.AddDays(1);

                events = events.Where(e =>
                    e.start_time < endDate);
            }


            // ==============================
            // LẤY DANH MỤC
            // ==============================

            ViewBag.Categories = db.event_categories
                .OrderBy(c => c.name)
                .ToList();


            // ==============================
            // GIỮ GIÁ TRỊ BỘ LỌC
            // ==============================

            ViewBag.SearchString = searchString;
            ViewBag.Filter = filter;
            ViewBag.CategoryId = categoryId;

            ViewBag.FromDate = fromDate.HasValue
                ? fromDate.Value.ToString("yyyy-MM-dd")
                : "";

            ViewBag.ToDate = toDate.HasValue
                ? toDate.Value.ToString("yyyy-MM-dd")
                : "";


            // ==============================
            // SẮP XẾP
            // ==============================

            events = events.OrderBy(e => e.start_time);

            return View(events.ToList());
        }

        // GET: Events/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(400);
            }

            var eventItem = db.events
                .FirstOrDefault(e => e.id == id);

            if (eventItem == null)
            {
                return HttpNotFound();
            }

            // ==============================
            // SỐ NGƯỜI ĐĂNG KÝ
            // ==============================

            int approvedCount = db.registrations.Count(r =>
                r.event_id == id &&
                r.status == "approved");

            int pendingCount = db.registrations.Count(r =>
                r.event_id == id &&
                r.status == "pending");

            ViewBag.ApprovedCount = approvedCount;
            ViewBag.PendingCount = pendingCount;


            // ==============================
            // KIỂM TRA TRẠNG THÁI ĐĂNG KÝ
            // CỦA NGƯỜI DÙNG HIỆN TẠI
            // ==============================

            ViewBag.RegistrationStatus = null;

            if (Session["UserId"] != null)
            {
                int userId = Convert.ToInt32(Session["UserId"]);

                var registration = db.registrations.FirstOrDefault(r =>
                    r.user_id == userId &&
                    r.event_id == id);

                if (registration != null)
                {
                    ViewBag.RegistrationStatus = registration.status;
                }
            }


            // ==============================
            // ĐÁNH GIÁ SỰ KIỆN
            // ==============================

            var feedbackList = db.feedbacks
                .Where(f => f.event_id == id)
                .OrderByDescending(f => f.created_at)
                .ToList();

            ViewBag.FeedbackList = feedbackList;

            // Có đánh giá
            if (feedbackList.Any())
            {
                double averageRating = feedbackList
                    .Average(f => f.rating);

                ViewBag.AverageRating = averageRating;
                ViewBag.FeedbackCount = feedbackList.Count;
            }
            else
            {
                // Chưa có đánh giá
                ViewBag.AverageRating = 0;
                ViewBag.FeedbackCount = 0;
            }


            return View(eventItem);
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