using quanlySukien;
using quanlySukien.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace quanlySukien.Controllers
{
    public class StaffRegistrationsController : Controller
    {
        private event_managementEntities db = new event_managementEntities();


        // =========================================================
        // KIỂM TRA CÁN BỘ ĐĂNG NHẬP
        // =========================================================
        private bool IsStaff()
        {
            return Session["UserId"] != null &&
                   Session["Role"] != null &&
                   Session["Role"].ToString() == "staff";
        }


        // =========================================================
        // DANH SÁCH ĐĂNG KÝ
        // /StaffRegistrations/Index?eventId=1
        // =========================================================
        public ActionResult Index(int? eventId)
        {
            if (!IsStaff())
            {
                return RedirectToAction("Login", "Account");
            }

            int userId =
                Convert.ToInt32(Session["UserId"]);

            // Lấy thông tin cán bộ
            var staff = db.users
                .FirstOrDefault(u => u.id == userId);

            if (staff == null)
            {
                return HttpNotFound();
            }

            // Cán bộ phải thuộc đơn vị
            if (!staff.department_id.HasValue)
            {
                ViewBag.Error =
                    "Tài khoản cán bộ chưa được phân công đơn vị.";

                return View(new List<registration>());
            }

            // =====================================================
            // LẤY DANH SÁCH SỰ KIỆN CỦA ĐƠN VỊ
            // =====================================================

            var events = db.events
                .Where(e =>
                    e.department_id ==
                    staff.department_id.Value)
                .OrderByDescending(e => e.start_time)
                .ToList();

            ViewBag.Events = events;

            // Nếu chưa chọn sự kiện
            if (!eventId.HasValue)
            {
                return View(new List<registration>());
            }

            // =====================================================
            // KIỂM TRA SỰ KIỆN CÓ THUỘC ĐƠN VỊ KHÔNG
            // =====================================================

            var eventItem = db.events
                .FirstOrDefault(e =>
                    e.id == eventId.Value &&
                    e.department_id ==
                    staff.department_id.Value);

            if (eventItem == null)
            {
                ViewBag.Error =
                    "Bạn không có quyền quản lý sự kiện này.";

                return View(new List<registration>());
            }

            ViewBag.SelectedEvent = eventItem;

            // =====================================================
            // LẤY DANH SÁCH ĐĂNG KÝ
            // =====================================================

            var registrations = db.registrations
                .Where(r => r.event_id == eventId.Value)
                .OrderBy(r => r.status)
                .ThenBy(r => r.registered_at)
                .ToList();

            return View(registrations);
        }


        // =========================================================
        // DUYỆT ĐĂNG KÝ
        // /StaffRegistrations/Approve/1
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Approve(int id)
        {
            if (!IsStaff())
            {
                return RedirectToAction("Login", "Account");
            }

            int userId =
                Convert.ToInt32(Session["UserId"]);

            var staff = db.users
                .FirstOrDefault(u => u.id == userId);

            if (staff == null)
            {
                return HttpNotFound();
            }

            if (!staff.department_id.HasValue)
            {
                TempData["Error"] =
                    "Tài khoản cán bộ chưa được phân công đơn vị.";

                return RedirectToAction("Index");
            }

            // =====================================================
            // LẤY ĐĂNG KÝ
            // =====================================================

            var registration = db.registrations
                .FirstOrDefault(r => r.id == id);

            if (registration == null)
            {
                return HttpNotFound();
            }

            // =====================================================
            // KIỂM TRA SỰ KIỆN THUỘC ĐƠN VỊ
            // =====================================================

            var eventItem = db.events
                .FirstOrDefault(e =>
                    e.id == registration.event_id &&
                    e.department_id ==
                    staff.department_id.Value);

            if (eventItem == null)
            {
                return new HttpStatusCodeResult(403);
            }

            // =====================================================
            // CHỈ DUYỆT ĐĂNG KÝ ĐANG PENDING
            // =====================================================

            if (registration.status != "pending")
            {
                TempData["Error"] =
                    "Đăng ký này không còn ở trạng thái chờ duyệt.";

                return RedirectToAction(
                    "Index",
                    new { eventId = registration.event_id });
            }

            // =====================================================
            // KIỂM TRA SỐ LƯỢNG
            // =====================================================

            if (eventItem.max_participants.HasValue)
            {
                int approvedCount = db.registrations
                    .Count(r =>
                        r.event_id == eventItem.id &&
                        r.status == "approved");

                if (approvedCount >=
                    eventItem.max_participants.Value)
                {
                    TempData["Error"] =
                        "Sự kiện đã đủ số lượng người tham gia.";

                    return RedirectToAction(
                        "Index",
                        new { eventId = eventItem.id });
                }
            }

            // =====================================================
            // DUYỆT
            // =====================================================

            registration.status = "approved";

            registration.approved_at =
                DateTime.Now;

            registration.approved_by =
                userId;

            registration.rejection_reason =
                null;

            db.SaveChanges();


            // =====================================================
            // TẠO THÔNG BÁO CHO SINH VIÊN
            // =====================================================

            var notification = new notification
            {
                user_id = registration.user_id,

                title = "Đăng ký sự kiện được duyệt",

                message =
                    "Đăng ký tham gia sự kiện \"" +
                    eventItem.title +
                    "\" của bạn đã được duyệt.",

                is_read = false,

                created_at = DateTime.Now
            };

            db.notifications.Add(notification);

            db.SaveChanges();


            TempData["Success"] =
                "Đã duyệt đăng ký thành công.";

            return RedirectToAction(
                "Index",
                new { eventId = eventItem.id });
        }


        // =========================================================
        // TỪ CHỐI ĐĂNG KÝ
        // GET: /StaffRegistrations/Reject/1
        // =========================================================
        public ActionResult Reject(int? id)
        {
            if (!IsStaff())
            {
                return RedirectToAction("Login", "Account");
            }

            if (id == null)
            {
                return new HttpStatusCodeResult(400);
            }

            int userId =
                Convert.ToInt32(Session["UserId"]);

            var staff = db.users
                .FirstOrDefault(u => u.id == userId);

            if (staff == null)
            {
                return HttpNotFound();
            }

            var registration = db.registrations
                .FirstOrDefault(r => r.id == id.Value);

            if (registration == null)
            {
                return HttpNotFound();
            }

            var eventItem = db.events
                .FirstOrDefault(e =>
                    e.id == registration.event_id &&
                    e.department_id ==
                    staff.department_id);

            if (eventItem == null)
            {
                return new HttpStatusCodeResult(403);
            }

            ViewBag.EventTitle =
                eventItem.title;

            ViewBag.Registration =
                registration;

            return View();
        }


        // =========================================================
        // POST TỪ CHỐI ĐĂNG KÝ
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Reject(
            int id,
            string rejection_reason)
        {
            if (!IsStaff())
            {
                return RedirectToAction("Login", "Account");
            }

            int userId =
                Convert.ToInt32(Session["UserId"]);

            var staff = db.users
                .FirstOrDefault(u => u.id == userId);

            if (staff == null)
            {
                return HttpNotFound();
            }

            var registration = db.registrations
                .FirstOrDefault(r => r.id == id);

            if (registration == null)
            {
                return HttpNotFound();
            }

            var eventItem = db.events
                .FirstOrDefault(e =>
                    e.id == registration.event_id &&
                    e.department_id ==
                    staff.department_id);

            if (eventItem == null)
            {
                return new HttpStatusCodeResult(403);
            }

            // =====================================================
            // CHỈ TỪ CHỐI PENDING
            // =====================================================

            if (registration.status != "pending")
            {
                TempData["Error"] =
                    "Đăng ký này không còn ở trạng thái chờ duyệt.";

                return RedirectToAction(
                    "Index",
                    new { eventId = registration.event_id });
            }

            // =====================================================
            // KIỂM TRA LÝ DO
            // =====================================================

            if (string.IsNullOrWhiteSpace(rejection_reason))
            {
                ViewBag.Error =
                    "Vui lòng nhập lý do từ chối.";

                ViewBag.EventTitle =
                    eventItem.title;

                ViewBag.Registration =
                    registration;

                return View();
            }

            // =====================================================
            // TỪ CHỐI
            // =====================================================

            registration.status =
                "rejected";

            registration.approved_at =
                null;

            registration.approved_by =
                userId;

            registration.rejection_reason =
                rejection_reason;

            db.SaveChanges();


            // =====================================================
            // TẠO THÔNG BÁO CHO SINH VIÊN
            // =====================================================

            var notification = new notification
            {
                user_id = registration.user_id,

                title = "Đăng ký sự kiện bị từ chối",

                message =
                    "Đăng ký tham gia sự kiện \"" +
                    eventItem.title +
                    "\" đã bị từ chối. Lý do: " +
                    rejection_reason,

                is_read = false,

                created_at = DateTime.Now
            };

            db.notifications.Add(notification);

            db.SaveChanges();


            TempData["Success"] =
                "Đã từ chối đăng ký.";

            return RedirectToAction(
                "Index",
                new { eventId = eventItem.id });
        }


        // =========================================================
        // GIẢI PHÓNG DATABASE
        // =========================================================
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