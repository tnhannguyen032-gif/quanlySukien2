using System;
using System.Linq;
using System.Web.Mvc;
using quanlySukien.Filters;
using quanlySukien.Models;

namespace quanlySukien.Areas.Admin.Controllers
{
    [AdminAuthorize]
    public class AdminEventsController : Controller
    {
        private event_managementEntities db =
            new event_managementEntities();


        // =====================================================
        // DANH SÁCH SỰ KIỆN
        // GET: Admin/Events
        // =====================================================

        public ActionResult Index(
            string searchString,
            string statusFilter)
        {
            var events = db.events.AsQueryable();

            // Tìm kiếm
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                events = events.Where(e =>
                    e.title.Contains(searchString) ||
                    e.location.Contains(searchString));
            }

            // Lọc trạng thái
            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                events = events.Where(e =>
                    e.status == statusFilter);
            }

            // Sắp xếp
            events = events
                .OrderBy(e => e.status)
                .ThenByDescending(e => e.created_at);

            // Danh sách Khoa / Viện / Ban
            ViewBag.Departments = db.departments
                .OrderBy(d => d.name)
                .ToList();

            // Danh sách người dùng
            ViewBag.Users = db.users
                .OrderBy(u => u.full_name)
                .ToList();

            // Giữ bộ lọc
            ViewBag.SearchString = searchString;
            ViewBag.StatusFilter = statusFilter;

            return View(events.ToList());
        }


        // =====================================================
        // CHI TIẾT SỰ KIỆN
        // GET: Admin/Events/Details/5
        // =====================================================

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


            // =================================================
            // KHOA / VIỆN / BAN
            // =================================================

            ViewBag.Department = db.departments
                .FirstOrDefault(d =>
                    d.id == eventItem.department_id);


            // =================================================
            // NGƯỜI TẠO
            // =================================================

            ViewBag.Creator = db.users
                .FirstOrDefault(u =>
                    u.id == eventItem.created_by);


            // =================================================
            // SỐ NGƯỜI ĐĂNG KÝ
            // =================================================

            ViewBag.ApprovedCount =
                db.registrations.Count(r =>
                    r.event_id == eventItem.id &&
                    r.status == "approved");

            ViewBag.PendingCount =
                db.registrations.Count(r =>
                    r.event_id == eventItem.id &&
                    r.status == "pending");


            return View(eventItem);
        }


        // =====================================================
        // DUYỆT SỰ KIỆN
        // POST: Admin/Events/Approve/5
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Approve(int id)
        {
            var eventItem = db.events
                .FirstOrDefault(e => e.id == id);

            if (eventItem == null)
            {
                return HttpNotFound();
            }


            // Chỉ duyệt sự kiện đang chờ
            if (eventItem.status != "pending")
            {
                TempData["Error"] =
                    "Sự kiện này không còn ở trạng thái chờ duyệt.";

                return RedirectToAction("Index");
            }


            // Chuyển sang đã duyệt
            eventItem.status = "approved";


            // =================================================
            // GỬI THÔNG BÁO CHO STAFF TẠO SỰ KIỆN
            // =================================================

            var notification = new notification
            {
                user_id = eventItem.created_by,

                title = "Sự kiện đã được phê duyệt",

                message =
        "Sự kiện \"" +
        eventItem.title +
        "\" đã được Admin phê duyệt.",

                is_read = false,

                created_at = DateTime.Now
            };

            db.notifications.Add(notification);


            db.SaveChanges();


            TempData["Success"] =
                "Đã phê duyệt sự kiện \"" +
                eventItem.title +
                "\".";


            return RedirectToAction("Index");
        }


        // =====================================================
        // TỪ CHỐI SỰ KIỆN
        // POST: Admin/Events/Reject/5
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Reject(
            int id,
            string rejection_reason)
        {
            var eventItem = db.events
                .FirstOrDefault(e => e.id == id);

            if (eventItem == null)
            {
                return HttpNotFound();
            }


            // Chỉ từ chối sự kiện đang chờ
            if (eventItem.status != "pending")
            {
                TempData["Error"] =
                    "Sự kiện này không còn ở trạng thái chờ duyệt.";

                return RedirectToAction("Index");
            }


            // =================================================
            // LƯU LÝ DO TỪ CHỐI
            // =================================================

            if (string.IsNullOrWhiteSpace(rejection_reason))
            {
                rejection_reason =
                    "Sự kiện chưa đáp ứng yêu cầu.";
            }

            eventItem.status = "rejected";
            eventItem.rejection_reason = rejection_reason;


            // =================================================
            // THÔNG BÁO CHO STAFF
            // =================================================

            var notification = new notification
            {
                user_id = eventItem.created_by,

                title = "Sự kiện bị từ chối",

                message =
        "Sự kiện \"" +
        eventItem.title +
        "\" đã bị Admin từ chối. " +
        "Lý do: " +
        rejection_reason,

                is_read = false,

                created_at = DateTime.Now
            };

            db.notifications.Add(notification);


            db.SaveChanges();


            TempData["Success"] =
                "Đã từ chối sự kiện \"" +
                eventItem.title +
                "\".";


            return RedirectToAction("Index");
        }


        // =====================================================
        // DISPOSE
        // =====================================================

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