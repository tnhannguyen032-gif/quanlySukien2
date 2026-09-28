using quanlySukien;
using quanlySukien.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;

namespace quanlySukien.Controllers
{
    public class StaffEventsController : Controller
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
        // GIỮ LẠI DỮ LIỆU FORM KHI CÓ LỖI
        // =========================================================
        private void KeepFormValues(
            string title,
            string description,
            int category_id,
            string location,
            string start_date,
            string start_time,
            string end_date,
            string end_time,
            string registration_date,
            string registration_time,
            int? max_participants)
        {
            ViewBag.TitleValue = title;
            ViewBag.DescriptionValue = description;
            ViewBag.LocationValue = location;

            ViewBag.StartDateValue = start_date;
            ViewBag.StartTimeValue = start_time;

            ViewBag.EndDateValue = end_date;
            ViewBag.EndTimeValue = end_time;

            ViewBag.RegistrationDateValue = registration_date;
            ViewBag.RegistrationTimeValue = registration_time;

            if (max_participants.HasValue)
            {
                ViewBag.MaxParticipantsValue =
                    max_participants.Value.ToString();
            }
            else
            {
                ViewBag.MaxParticipantsValue = "";
            }

            LoadCategories(category_id);
        }

        // =========================================================
        // CHUYỂN NGÀY + GIỜ THÀNH DATETIME
        // Định dạng ngày: dd/MM/yyyy
        // Định dạng giờ: HH:mm
        // =========================================================
        private bool TryParseDateTime(
            string dateText,
            string timeText,
            out DateTime result)
        {
            result = DateTime.MinValue;

            if (string.IsNullOrWhiteSpace(dateText) ||
                string.IsNullOrWhiteSpace(timeText))
            {
                return false;
            }

            string value =
                dateText.Trim() + " " + timeText.Trim();

            string[] formats =
            {
                "dd/MM/yyyy HH:mm",
                "dd/MM/yyyy H:mm",
                "d/M/yyyy HH:mm",
                "d/M/yyyy H:mm"
            };

            return DateTime.TryParseExact(
                value,
                formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out result
            );
        }

        // =========================================================
        // DANH SÁCH SỰ KIỆN CỦA CÁN BỘ
        // =========================================================
        public ActionResult Index()
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
                ViewBag.Error =
                    "Tài khoản cán bộ chưa được phân công đơn vị.";

                return View(new List<@event>());
            }

            var events = db.events
                .Where(e =>
                    e.department_id ==
                    staff.department_id.Value)
                .OrderByDescending(e => e.created_at)
                .ToList();

            return View(events);
        }

        // =========================================================
        // GET: StaffEvents/Details/5
        // =========================================================
        public ActionResult Details(int? id)
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

            var eventItem = db.events
                .FirstOrDefault(e =>
                    e.id == id &&
                    e.department_id == staff.department_id);

            if (eventItem == null)
            {
                return HttpNotFound();
            }

            return View(eventItem);
        }

        // =========================================================
        // GET: StaffEvents/Create
        // =========================================================
        public ActionResult Create()
        {
            if (!IsStaff())
            {
                return RedirectToAction("Login", "Account");
            }

            LoadCategories();

            return View();
        }

        // =========================================================
        // POST: StaffEvents/Create
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(
            string title,
            string description,
            int category_id,
            string location,
            string start_date,
            string start_time,
            string end_date,
            string end_time,
            string registration_date,
            string registration_time,
            int? max_participants)
        {
            // =====================================================
            // KIỂM TRA ĐĂNG NHẬP
            // =====================================================
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

            // =====================================================
            // KIỂM TRA ĐƠN VỊ
            // =====================================================
            if (!staff.department_id.HasValue)
            {
                ViewBag.Error =
                    "Tài khoản cán bộ chưa được phân công đơn vị.";

                KeepFormValues(
                    title,
                    description,
                    category_id,
                    location,
                    start_date,
                    start_time,
                    end_date,
                    end_time,
                    registration_date,
                    registration_time,
                    max_participants
                );

                return View();
            }

            // =====================================================
            // KIỂM TRA TÊN
            // =====================================================
            if (string.IsNullOrWhiteSpace(title))
            {
                ViewBag.Error =
                    "Vui lòng nhập tên sự kiện.";

                KeepFormValues(
                    title,
                    description,
                    category_id,
                    location,
                    start_date,
                    start_time,
                    end_date,
                    end_time,
                    registration_date,
                    registration_time,
                    max_participants
                );

                return View();
            }

            // =====================================================
            // KIỂM TRA ĐỊA ĐIỂM
            // =====================================================
            if (string.IsNullOrWhiteSpace(location))
            {
                ViewBag.Error =
                    "Vui lòng nhập địa điểm tổ chức.";

                KeepFormValues(
                    title,
                    description,
                    category_id,
                    location,
                    start_date,
                    start_time,
                    end_date,
                    end_time,
                    registration_date,
                    registration_time,
                    max_participants
                );

                return View();
            }

            // =====================================================
            // KIỂM TRA NGÀY / GIỜ BẮT ĐẦU
            // =====================================================
            if (string.IsNullOrWhiteSpace(start_date) ||
                string.IsNullOrWhiteSpace(start_time))
            {
                ViewBag.Error =
                    "Vui lòng nhập đầy đủ ngày và giờ bắt đầu.";

                KeepFormValues(
                    title,
                    description,
                    category_id,
                    location,
                    start_date,
                    start_time,
                    end_date,
                    end_time,
                    registration_date,
                    registration_time,
                    max_participants
                );

                return View();
            }

            DateTime startDateTime;

            bool startValid =
                TryParseDateTime(
                    start_date,
                    start_time,
                    out startDateTime
                );

            if (!startValid)
            {
                ViewBag.Error =
                    "Ngày hoặc giờ bắt đầu không hợp lệ. Vui lòng nhập ngày dạng dd/MM/yyyy và giờ dạng HH:mm.";

                KeepFormValues(
                    title,
                    description,
                    category_id,
                    location,
                    start_date,
                    start_time,
                    end_date,
                    end_time,
                    registration_date,
                    registration_time,
                    max_participants
                );

                return View();
            }

            // =====================================================
            // KIỂM TRA NGÀY / GIỜ KẾT THÚC
            // =====================================================
            if (string.IsNullOrWhiteSpace(end_date) ||
                string.IsNullOrWhiteSpace(end_time))
            {
                ViewBag.Error =
                    "Vui lòng nhập đầy đủ ngày và giờ kết thúc.";

                KeepFormValues(
                    title,
                    description,
                    category_id,
                    location,
                    start_date,
                    start_time,
                    end_date,
                    end_time,
                    registration_date,
                    registration_time,
                    max_participants
                );

                return View();
            }

            DateTime endDateTime;

            bool endValid =
                TryParseDateTime(
                    end_date,
                    end_time,
                    out endDateTime
                );

            if (!endValid)
            {
                ViewBag.Error =
                    "Ngày hoặc giờ kết thúc không hợp lệ. Vui lòng nhập ngày dạng dd/MM/yyyy và giờ dạng HH:mm.";

                KeepFormValues(
                    title,
                    description,
                    category_id,
                    location,
                    start_date,
                    start_time,
                    end_date,
                    end_time,
                    registration_date,
                    registration_time,
                    max_participants
                );

                return View();
            }

            // =====================================================
            // KIỂM TRA KẾT THÚC > BẮT ĐẦU
            // =====================================================
            if (endDateTime <= startDateTime)
            {
                ViewBag.Error =
                    "Thời gian kết thúc phải lớn hơn thời gian bắt đầu.";

                KeepFormValues(
                    title,
                    description,
                    category_id,
                    location,
                    start_date,
                    start_time,
                    end_date,
                    end_time,
                    registration_date,
                    registration_time,
                    max_participants
                );

                return View();
            }

            // =====================================================
            // HẠN ĐĂNG KÝ
            // =====================================================
            DateTime? registrationDateTime = null;

            if (!string.IsNullOrWhiteSpace(registration_date) ||
                !string.IsNullOrWhiteSpace(registration_time))
            {
                // Phải nhập đủ cả ngày và giờ
                if (string.IsNullOrWhiteSpace(registration_date) ||
                    string.IsNullOrWhiteSpace(registration_time))
                {
                    ViewBag.Error =
                        "Vui lòng nhập đầy đủ ngày và giờ hạn đăng ký.";

                    KeepFormValues(
                        title,
                        description,
                        category_id,
                        location,
                        start_date,
                        start_time,
                        end_date,
                        end_time,
                        registration_date,
                        registration_time,
                        max_participants
                    );

                    return View();
                }

                DateTime tempRegistrationDateTime;

                bool registrationValid =
                    TryParseDateTime(
                        registration_date,
                        registration_time,
                        out tempRegistrationDateTime
                    );

                if (!registrationValid)
                {
                    ViewBag.Error =
                        "Ngày hoặc giờ hạn đăng ký không hợp lệ. Vui lòng nhập ngày dạng dd/MM/yyyy và giờ dạng HH:mm.";

                    KeepFormValues(
                        title,
                        description,
                        category_id,
                        location,
                        start_date,
                        start_time,
                        end_date,
                        end_time,
                        registration_date,
                        registration_time,
                        max_participants
                    );

                    return View();
                }

                registrationDateTime =
                    tempRegistrationDateTime;

                // Hạn đăng ký phải <= thời gian bắt đầu
                if (registrationDateTime.Value > startDateTime)
                {
                    ViewBag.Error =
                        "Hạn đăng ký không được sau thời gian bắt đầu sự kiện.";

                    KeepFormValues(
                        title,
                        description,
                        category_id,
                        location,
                        start_date,
                        start_time,
                        end_date,
                        end_time,
                        registration_date,
                        registration_time,
                        max_participants
                    );

                    return View();
                }
            }

            // =====================================================
            // KIỂM TRA SỐ LƯỢNG
            // =====================================================
            if (max_participants.HasValue &&
                max_participants.Value <= 0)
            {
                ViewBag.Error =
                    "Số lượng người tham gia phải lớn hơn 0.";

                KeepFormValues(
                    title,
                    description,
                    category_id,
                    location,
                    start_date,
                    start_time,
                    end_date,
                    end_time,
                    registration_date,
                    registration_time,
                    max_participants
                );

                return View();
            }

            // =====================================================
            // KIỂM TRA LOẠI SỰ KIỆN
            // =====================================================
            var category = db.event_categories
                .FirstOrDefault(c =>
                    c.id == category_id &&
                    c.status == "active");

            if (category == null)
            {
                ViewBag.Error =
                    "Loại sự kiện không hợp lệ.";

                KeepFormValues(
                    title,
                    description,
                    category_id,
                    location,
                    start_date,
                    start_time,
                    end_date,
                    end_time,
                    registration_date,
                    registration_time,
                    max_participants
                );

                return View();
            }

            // =====================================================
            // TẠO SỰ KIỆN
            // =====================================================
            var newEvent = new @event
            {
                title = title,
                description = description,
                category_id = category_id,

                department_id =
                    staff.department_id.Value,

                created_by = userId,

                location = location,

                start_time = startDateTime,
                end_time = endDateTime,

                registration_deadline =
                    registrationDateTime,

                max_participants =
                    max_participants,

                status = "pending",

                rejection_reason = null,

                created_at = DateTime.Now,

                updated_at = null
            };

            // =====================================================
            // LƯU DATABASE
            // =====================================================
            db.events.Add(newEvent);

            db.SaveChanges();

            // =====================================================
            // THÔNG BÁO THÀNH CÔNG
            // =====================================================
            TempData["Success"] =
                "Tạo sự kiện thành công. Sự kiện đang chờ Admin kiểm duyệt.";

            return RedirectToAction("Index");
        }

        // =========================================================
        // GET: StaffEvents/Edit/5
        // =========================================================
        public ActionResult Edit(int? id)
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

            var eventItem = db.events
                .FirstOrDefault(e =>
                    e.id == id &&
                    e.department_id == staff.department_id);

            if (eventItem == null)
            {
                return HttpNotFound();
            }

            // Không cho sửa sự kiện đã hoàn thành
            if (eventItem.status == "finished")
            {
                TempData["Error"] =
                    "Sự kiện đã kết thúc nên không thể chỉnh sửa.";

                return RedirectToAction("Index");
            }

            // =====================================================
            // HIỂN THỊ NGÀY / GIỜ THEO KIỂU VIỆT NAM
            // =====================================================
            ViewBag.StartDateValue =
                eventItem.start_time.ToString("dd/MM/yyyy");

            ViewBag.StartTimeValue =
                eventItem.start_time.ToString("HH:mm");

            ViewBag.EndDateValue =
                eventItem.end_time.ToString("dd/MM/yyyy");

            ViewBag.EndTimeValue =
                eventItem.end_time.ToString("HH:mm");

            if (eventItem.registration_deadline.HasValue)
            {
                ViewBag.RegistrationDateValue =
                    eventItem.registration_deadline.Value
                        .ToString("dd/MM/yyyy");

                ViewBag.RegistrationTimeValue =
                    eventItem.registration_deadline.Value
                        .ToString("HH:mm");
            }
            else
            {
                ViewBag.RegistrationDateValue = "";
                ViewBag.RegistrationTimeValue = "";
            }

            ViewBag.MaxParticipantsValue =
                eventItem.max_participants?.ToString();

            ViewBag.TitleValue =
                eventItem.title;

            ViewBag.DescriptionValue =
                eventItem.description;

            ViewBag.LocationValue =
                eventItem.location;

            LoadCategories(eventItem.category_id);

            return View(eventItem);
        }

        // =========================================================
        // POST: StaffEvents/Edit
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(
            int id,
            string title,
            string description,
            int category_id,
            string location,
            string start_date,
            string start_time,
            string end_date,
            string end_time,
            string registration_date,
            string registration_time,
            int? max_participants)
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

            var eventItem = db.events
                .FirstOrDefault(e =>
                    e.id == id &&
                    e.department_id == staff.department_id);

            if (eventItem == null)
            {
                return HttpNotFound();
            }

            // =====================================================
            // GIỮ DỮ LIỆU FORM
            // =====================================================
            ViewBag.TitleValue = title;
            ViewBag.DescriptionValue = description;
            ViewBag.LocationValue = location;

            ViewBag.StartDateValue = start_date;
            ViewBag.StartTimeValue = start_time;

            ViewBag.EndDateValue = end_date;
            ViewBag.EndTimeValue = end_time;

            ViewBag.RegistrationDateValue = registration_date;
            ViewBag.RegistrationTimeValue = registration_time;

            ViewBag.MaxParticipantsValue =
                max_participants?.ToString();

            // =====================================================
            // KIỂM TRA CƠ BẢN
            // =====================================================
            if (string.IsNullOrWhiteSpace(title))
            {
                ViewBag.Error =
                    "Vui lòng nhập tên sự kiện.";

                LoadCategories(category_id);

                return View(eventItem);
            }

            if (string.IsNullOrWhiteSpace(location))
            {
                ViewBag.Error =
                    "Vui lòng nhập địa điểm.";

                LoadCategories(category_id);

                return View(eventItem);
            }

            // =====================================================
            // KIỂM TRA NGÀY / GIỜ BẮT ĐẦU
            // =====================================================
            if (string.IsNullOrWhiteSpace(start_date) ||
                string.IsNullOrWhiteSpace(start_time))
            {
                ViewBag.Error =
                    "Vui lòng nhập đầy đủ ngày và giờ bắt đầu.";

                LoadCategories(category_id);

                return View(eventItem);
            }

            DateTime startDateTime;

            bool startValid =
                TryParseDateTime(
                    start_date,
                    start_time,
                    out startDateTime
                );

            if (!startValid)
            {
                ViewBag.Error =
                    "Ngày hoặc giờ bắt đầu không hợp lệ. Vui lòng nhập ngày dạng dd/MM/yyyy và giờ dạng HH:mm.";

                LoadCategories(category_id);

                return View(eventItem);
            }

            // =====================================================
            // KIỂM TRA NGÀY / GIỜ KẾT THÚC
            // =====================================================
            if (string.IsNullOrWhiteSpace(end_date) ||
                string.IsNullOrWhiteSpace(end_time))
            {
                ViewBag.Error =
                    "Vui lòng nhập đầy đủ ngày và giờ kết thúc.";

                LoadCategories(category_id);

                return View(eventItem);
            }

            DateTime endDateTime;

            bool endValid =
                TryParseDateTime(
                    end_date,
                    end_time,
                    out endDateTime
                );

            if (!endValid)
            {
                ViewBag.Error =
                    "Ngày hoặc giờ kết thúc không hợp lệ. Vui lòng nhập ngày dạng dd/MM/yyyy và giờ dạng HH:mm.";

                LoadCategories(category_id);

                return View(eventItem);
            }

            // =====================================================
            // KIỂM TRA THỜI GIAN
            // =====================================================
            if (endDateTime <= startDateTime)
            {
                ViewBag.Error =
                    "Thời gian kết thúc phải lớn hơn thời gian bắt đầu.";

                LoadCategories(category_id);

                return View(eventItem);
            }

            // =====================================================
            // HẠN ĐĂNG KÝ
            // =====================================================
            DateTime? registrationDateTime = null;

            if (!string.IsNullOrWhiteSpace(registration_date) ||
                !string.IsNullOrWhiteSpace(registration_time))
            {
                if (string.IsNullOrWhiteSpace(registration_date) ||
                    string.IsNullOrWhiteSpace(registration_time))
                {
                    ViewBag.Error =
                        "Vui lòng nhập đầy đủ ngày và giờ hạn đăng ký.";

                    LoadCategories(category_id);

                    return View(eventItem);
                }

                DateTime tempRegistrationDateTime;

                bool registrationValid =
                    TryParseDateTime(
                        registration_date,
                        registration_time,
                        out tempRegistrationDateTime
                    );

                if (!registrationValid)
                {
                    ViewBag.Error =
                        "Ngày hoặc giờ hạn đăng ký không hợp lệ. Vui lòng nhập ngày dạng dd/MM/yyyy và giờ dạng HH:mm.";

                    LoadCategories(category_id);

                    return View(eventItem);
                }

                registrationDateTime =
                    tempRegistrationDateTime;
            }

            // =====================================================
            // KIỂM TRA HẠN ĐĂNG KÝ
            // =====================================================
            if (registrationDateTime.HasValue &&
                registrationDateTime.Value > startDateTime)
            {
                ViewBag.Error =
                    "Hạn đăng ký không được sau thời gian bắt đầu sự kiện.";

                LoadCategories(category_id);

                return View(eventItem);
            }

            // =====================================================
            // KIỂM TRA SỐ LƯỢNG
            // =====================================================
            if (max_participants.HasValue &&
                max_participants.Value <= 0)
            {
                ViewBag.Error =
                    "Số lượng người tham gia phải lớn hơn 0.";

                LoadCategories(category_id);

                return View(eventItem);
            }

            // =====================================================
            // KIỂM TRA LOẠI SỰ KIỆN
            // =====================================================
            var category = db.event_categories
                .FirstOrDefault(c =>
                    c.id == category_id &&
                    c.status == "active");

            if (category == null)
            {
                ViewBag.Error =
                    "Loại sự kiện không hợp lệ.";

                LoadCategories(category_id);

                return View(eventItem);
            }

            // =====================================================
            // CẬP NHẬT SỰ KIỆN
            // =====================================================
            eventItem.title = title;

            eventItem.description =
                description;

            eventItem.category_id =
                category_id;

            eventItem.location =
                location;

            eventItem.start_time =
                startDateTime;

            eventItem.end_time =
                endDateTime;

            eventItem.registration_deadline =
                registrationDateTime;

            eventItem.max_participants =
                max_participants;

            eventItem.updated_at =
                DateTime.Now;

            // Nếu sự kiện bị từ chối,
            // sửa lại thì gửi về trạng thái chờ duyệt
            if (eventItem.status == "rejected")
            {
                eventItem.status = "pending";
                eventItem.rejection_reason = null;
            }

            db.SaveChanges();

            TempData["Success"] =
                "Cập nhật sự kiện thành công.";

            return RedirectToAction("Index");
        }

        // =========================================================
        // GET: StaffEvents/Delete/5
        // =========================================================
        public ActionResult Delete(int? id)
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

            var eventItem = db.events
                .FirstOrDefault(e =>
                    e.id == id &&
                    e.department_id == staff.department_id);

            if (eventItem == null)
            {
                return HttpNotFound();
            }

            int registrationCount =
                db.registrations.Count(r =>
                    r.event_id == id);

            ViewBag.RegistrationCount =
                registrationCount;

            return View(eventItem);
        }

        // =========================================================
        // POST: StaffEvents/Delete/5
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
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

            var eventItem = db.events
                .FirstOrDefault(e =>
                    e.id == id &&
                    e.department_id == staff.department_id);

            if (eventItem == null)
            {
                return HttpNotFound();
            }

            // Không cho xóa nếu đã có người đăng ký
            bool hasRegistrations =
                db.registrations
                    .Any(r => r.event_id == id);

            if (hasRegistrations)
            {
                TempData["Error"] =
                    "Không thể xóa sự kiện vì đã có sinh viên đăng ký.";

                return RedirectToAction("Index");
            }

            db.events.Remove(eventItem);

            db.SaveChanges();

            TempData["Success"] =
                "Xóa sự kiện thành công.";

            return RedirectToAction("Index");
        }

        // =========================================================
        // LOAD DANH SÁCH LOẠI SỰ KIỆN
        // =========================================================
        private void LoadCategories(int? selectedId = null)
        {
            ViewBag.CategoryList =
                new SelectList(
                    db.event_categories
                        .Where(c => c.status == "active")
                        .OrderBy(c => c.name)
                        .ToList(),
                    "id",
                    "name",
                    selectedId
                );
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