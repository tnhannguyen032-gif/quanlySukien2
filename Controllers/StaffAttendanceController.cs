using quanlySukien;
using quanlySukien.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace quanlySukien.Controllers
{
    public class StaffAttendanceController : Controller
    {
        private event_managementEntities db =
            new event_managementEntities();


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
        // DANH SÁCH ĐIỂM DANH
        // /StaffAttendance/Index?eventId=1
        // =========================================================
        public ActionResult Index(int? eventId)
        {
            if (!IsStaff())
            {
                return RedirectToAction("Login", "Account");
            }

            int userId =
                Convert.ToInt32(Session["UserId"]);

            // Lấy cán bộ đang đăng nhập
            var staff = db.users
                .FirstOrDefault(u => u.id == userId);

            if (staff == null)
            {
                return HttpNotFound();
            }

            // Cán bộ phải thuộc một đơn vị
            if (!staff.department_id.HasValue)
            {
                ViewBag.Error =
                    "Tài khoản cán bộ chưa được phân công đơn vị.";

                return View(new List<registration>());
            }


            // =====================================================
            // LẤY CÁC SỰ KIỆN CỦA ĐƠN VỊ
            // =====================================================

            var events = db.events
                .Where(e =>
                    e.department_id ==
                    staff.department_id.Value)
                .OrderByDescending(e => e.start_time)
                .ToList();

            ViewBag.Events = events;


            // Chưa chọn sự kiện
            if (!eventId.HasValue)
            {
                return View(new List<registration>());
            }


            // =====================================================
            // KIỂM TRA SỰ KIỆN THUỘC ĐƠN VỊ
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
            // CHỈ LẤY SINH VIÊN ĐÃ ĐƯỢC DUYỆT
            // =====================================================

            var registrations = db.registrations
                .Where(r =>
                    r.event_id == eventId.Value &&
                    r.status == "approved")
                .OrderBy(r => r.registered_at)
                .ToList();


            // =====================================================
            // TẠO DICTIONARY TRẠNG THÁI ĐIỂM DANH
            // =====================================================

            var registrationIds =
                registrations.Select(r => r.id).ToList();

            var attendanceList = db.attendances
                .Where(a =>
                    registrationIds.Contains(a.registration_id))
                .ToList();

            ViewBag.AttendanceList =
                attendanceList;


            return View(registrations);
        }


        // =========================================================
        // ĐIỂM DANH CÓ MẶT
        // /StaffAttendance/CheckIn/5
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CheckIn(int id)
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
            // KIỂM TRA ĐĂNG KÝ THUỘC SỰ KIỆN CỦA ĐƠN VỊ
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
            // CHỈ ĐIỂM DANH NGƯỜI ĐÃ ĐƯỢC DUYỆT
            // =====================================================

            if (registration.status != "approved")
            {
                TempData["Error"] =
                    "Chỉ sinh viên đã được duyệt mới có thể điểm danh.";

                return RedirectToAction(
                    "Index",
                    new { eventId = registration.event_id });
            }


            // =====================================================
            // KIỂM TRA ĐÃ ĐIỂM DANH CHƯA
            // =====================================================

            var attendance = db.attendances
                .FirstOrDefault(a =>
                    a.registration_id == registration.id);

            if (attendance == null)
            {
                // Chưa có bản ghi điểm danh
                attendance = new attendance
                {
                    registration_id = registration.id,

                    qr_code =
                        "QR-" +
                        registration.user_id +
                        "-" +
                        eventItem.id,

                    check_in_time =
                        DateTime.Now,

                    photo = null,

                    status = "present"
                };

                db.attendances.Add(attendance);
            }
            else
            {
                // Đã có bản ghi → cập nhật lại có mặt
                attendance.status = "present";

                attendance.check_in_time =
                    DateTime.Now;
            }

            db.SaveChanges();


            TempData["Success"] =
                "Điểm danh sinh viên thành công.";

            return RedirectToAction(
                "Index",
                new { eventId = registration.event_id });
        }


        // =========================================================
        // ĐÁNH DẤU VẮNG
        // /StaffAttendance/Absent/5
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Absent(int id)
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
            // KIỂM TRA QUYỀN
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
            // CHỈ XỬ LÝ REGISTRATION ĐÃ DUYỆT
            // =====================================================

            if (registration.status != "approved")
            {
                TempData["Error"] =
                    "Chỉ sinh viên đã được duyệt mới có thể điểm danh.";

                return RedirectToAction(
                    "Index",
                    new { eventId = registration.event_id });
            }


            // =====================================================
            // TÌM BẢN GHI ĐIỂM DANH
            // =====================================================

            var attendance = db.attendances
                .FirstOrDefault(a =>
                    a.registration_id == registration.id);

            if (attendance == null)
            {
                attendance = new attendance
                {
                    registration_id = registration.id,

                    qr_code =
                        "QR-" +
                        registration.user_id +
                        "-" +
                        eventItem.id,

                    check_in_time = null,

                    photo = null,

                    status = "absent"
                };

                db.attendances.Add(attendance);
            }
            else
            {
                attendance.status = "absent";

                attendance.check_in_time = null;
            }

            db.SaveChanges();


            TempData["Success"] =
                "Đã đánh dấu sinh viên vắng.";

            return RedirectToAction(
                "Index",
                new { eventId = registration.event_id });
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