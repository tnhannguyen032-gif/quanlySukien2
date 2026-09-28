using quanlySukien;
using quanlySukien.Filters;
using quanlySukien.Models;
using System;
using System.Linq;
using System.Web.Mvc;

namespace quanlySukien.Areas.Admin.Controllers
{
    [AdminAuthorize]
    public class UsersController : Controller
    {
        private event_managementEntities db =
            new event_managementEntities();

        // =========================================================
        // INDEX - DANH SÁCH NGƯỜI DÙNG
        // =========================================================
        public ActionResult Index(
            string keyword,
            string role,
            int? departmentId)
        {
            var users = db.users.AsQueryable();

            // Tìm kiếm
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();

                users = users.Where(u =>
                    u.username.Contains(keyword) ||
                    u.full_name.Contains(keyword) ||
                    u.email.Contains(keyword) ||
                    (u.phone != null &&
                     u.phone.Contains(keyword)));
            }

            // Lọc vai trò
            if (!string.IsNullOrWhiteSpace(role))
            {
                users = users.Where(u => u.role == role);
            }

            // Lọc đơn vị
            if (departmentId.HasValue)
            {
                users = users.Where(u =>
                    u.department_id == departmentId.Value);
            }

            var userList = users
                .OrderBy(u => u.role)
                .ThenBy(u => u.full_name)
                .ToList();

            // Dữ liệu cho dropdown đơn vị
            ViewBag.DepartmentList =
                new SelectList(
                    db.departments
                        .OrderBy(d => d.name)
                        .ToList(),
                    "id",
                    "name",
                    departmentId
                );

            ViewBag.Keyword = keyword;
            ViewBag.Role = role;
            ViewBag.DepartmentId = departmentId;

            // Thống kê
            ViewBag.TotalUsers = db.users.Count();

            ViewBag.TotalStudents =
                db.users.Count(u => u.role == "student");

            ViewBag.TotalStaff =
                db.users.Count(u => u.role == "staff");

            ViewBag.TotalAdmins =
                db.users.Count(u => u.role == "admin");

            return View(userList);
        }

        // =========================================================
        // GET: Admin/Users/CreateStaff
        // =========================================================
        [HttpGet]
        public ActionResult CreateStaff()
        {
            LoadDepartments();

            return View();
        }

        // =========================================================
        // POST: Admin/Users/CreateStaff
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateStaff(
            string username,
            string password,
            string confirmPassword,
            string full_name,
            string email,
            string phone,
            int? department_id)
        {
            // =====================================================
            // KIỂM TRA TÊN ĐĂNG NHẬP
            // =====================================================
            if (string.IsNullOrWhiteSpace(username))
            {
                ViewBag.Error =
                    "Vui lòng nhập tên đăng nhập.";

                LoadDepartments(department_id);

                return View();
            }

            username = username.Trim();

            bool usernameExists =
                db.users.Any(u =>
                    u.username == username);

            if (usernameExists)
            {
                ViewBag.Error =
                    "Tên đăng nhập đã tồn tại. Vui lòng chọn tên khác.";

                LoadDepartments(department_id);

                return View();
            }

            // =====================================================
            // KIỂM TRA MẬT KHẨU
            // =====================================================
            if (string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error =
                    "Vui lòng nhập mật khẩu.";

                LoadDepartments(department_id);

                return View();
            }

            if (password.Length < 6)
            {
                ViewBag.Error =
                    "Mật khẩu phải có ít nhất 6 ký tự.";

                LoadDepartments(department_id);

                return View();
            }

            if (password != confirmPassword)
            {
                ViewBag.Error =
                    "Mật khẩu xác nhận không khớp.";

                LoadDepartments(department_id);

                return View();
            }

            // =====================================================
            // KIỂM TRA HỌ TÊN
            // =====================================================
            if (string.IsNullOrWhiteSpace(full_name))
            {
                ViewBag.Error =
                    "Vui lòng nhập họ và tên.";

                LoadDepartments(department_id);

                return View();
            }

            // =====================================================
            // KIỂM TRA EMAIL
            // =====================================================
            if (string.IsNullOrWhiteSpace(email))
            {
                ViewBag.Error =
                    "Vui lòng nhập email.";

                LoadDepartments(department_id);

                return View();
            }

            email = email.Trim();

            bool emailExists =
                db.users.Any(u =>
                    u.email == email);

            if (emailExists)
            {
                ViewBag.Error =
                    "Email này đã được sử dụng.";

                LoadDepartments(department_id);

                return View();
            }

            // =====================================================
            // STAFF BẮT BUỘC PHẢI CÓ ĐƠN VỊ
            // =====================================================
            if (!department_id.HasValue)
            {
                ViewBag.Error =
                    "Vui lòng chọn đơn vị công tác cho Staff.";

                LoadDepartments(department_id);

                return View();
            }

            // Kiểm tra đơn vị có tồn tại
            var department =
                db.departments.FirstOrDefault(
                    d => d.id == department_id.Value);

            if (department == null)
            {
                ViewBag.Error =
                    "Đơn vị được chọn không tồn tại.";

                LoadDepartments(department_id);

                return View();
            }

            // =====================================================
            // TẠO TÀI KHOẢN STAFF
            // =====================================================
            var staff = new user
            {
                username = username,

                password = password,

                full_name = full_name.Trim(),

                email = email,

                phone = string.IsNullOrWhiteSpace(phone)
                    ? null
                    : phone.Trim(),

                // QUAN TRỌNG:
                // Không cho người nhập role.
                role = "staff",

                department_id =
                    department_id.Value,

                status = "active",

                created_at = DateTime.Now
            };

            db.users.Add(staff);

            db.SaveChanges();

            TempData["Success"] =
                "Tạo tài khoản Staff thành công.";

            return RedirectToAction("Index");
        }

        // =========================================================
        // GET: Admin/Users/Edit/5
        // =========================================================
        [HttpGet]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(400);
            }

            var userItem =
                db.users.FirstOrDefault(
                    u => u.id == id.Value);

            if (userItem == null)
            {
                return HttpNotFound();
            }

            // Không cho sửa tài khoản Admin
            // từ giao diện quản lý người dùng.
            if (userItem.role == "admin")
            {
                TempData["Error"] =
                    "Không thể chỉnh sửa tài khoản Admin tại đây.";

                return RedirectToAction("Index");
            }

            LoadDepartments(userItem.department_id);

            ViewBag.RoleName =
                GetRoleName(userItem.role);

            return View(userItem);
        }

        // =========================================================
        // POST: Admin/Users/Edit
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(
            int id,
            string full_name,
            string email,
            string phone,
            int? department_id,
            string newPassword,
            string confirmPassword)
        {
            var userItem =
                db.users.FirstOrDefault(
                    u => u.id == id);

            if (userItem == null)
            {
                return HttpNotFound();
            }

            // Không cho sửa Admin
            if (userItem.role == "admin")
            {
                TempData["Error"] =
                    "Không thể chỉnh sửa tài khoản Admin tại đây.";

                return RedirectToAction("Index");
            }

            // =====================================================
            // GIỮ DỮ LIỆU FORM
            // =====================================================
            ViewBag.FullNameValue = full_name;
            ViewBag.EmailValue = email;
            ViewBag.PhoneValue = phone;
            ViewBag.RoleName =
                GetRoleName(userItem.role);

            // =====================================================
            // HỌ TÊN
            // =====================================================
            if (string.IsNullOrWhiteSpace(full_name))
            {
                ViewBag.Error =
                    "Vui lòng nhập họ và tên.";

                LoadDepartments(department_id);

                return View(userItem);
            }

            // =====================================================
            // EMAIL
            // =====================================================
            if (string.IsNullOrWhiteSpace(email))
            {
                ViewBag.Error =
                    "Vui lòng nhập email.";

                LoadDepartments(department_id);

                return View(userItem);
            }

            email = email.Trim();

            bool duplicateEmail =
                db.users.Any(u =>
                    u.email == email &&
                    u.id != id);

            if (duplicateEmail)
            {
                ViewBag.Error =
                    "Email này đã được sử dụng bởi tài khoản khác.";

                LoadDepartments(department_id);

                return View(userItem);
            }

            // =====================================================
            // ĐƠN VỊ
            // =====================================================
            if (userItem.role == "staff")
            {
                if (!department_id.HasValue)
                {
                    ViewBag.Error =
                        "Staff phải được phân công đơn vị.";

                    LoadDepartments(department_id);

                    return View(userItem);
                }

                var department =
                    db.departments.FirstOrDefault(
                        d => d.id == department_id.Value);

                if (department == null)
                {
                    ViewBag.Error =
                        "Đơn vị được chọn không tồn tại.";

                    LoadDepartments(department_id);

                    return View(userItem);
                }

                userItem.department_id =
                    department_id.Value;
            }
            else
            {
                // Student có thể có hoặc không có đơn vị
                userItem.department_id =
                    department_id;
            }

            // =====================================================
            // CẬP NHẬT THÔNG TIN
            // =====================================================
            userItem.full_name =
                full_name.Trim();

            userItem.email =
                email;

            userItem.phone =
                string.IsNullOrWhiteSpace(phone)
                    ? null
                    : phone.Trim();

            // =====================================================
            // ĐỔI MẬT KHẨU - KHÔNG BẮT BUỘC
            // =====================================================
            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                if (newPassword.Length < 6)
                {
                    ViewBag.Error =
                        "Mật khẩu mới phải có ít nhất 6 ký tự.";

                    LoadDepartments(department_id);

                    return View(userItem);
                }

                if (newPassword != confirmPassword)
                {
                    ViewBag.Error =
                        "Mật khẩu xác nhận không khớp.";

                    LoadDepartments(department_id);

                    return View(userItem);
                }

                userItem.password =
                    newPassword;
            }

            db.SaveChanges();

            TempData["Success"] =
                "Cập nhật thông tin người dùng thành công.";

            return RedirectToAction("Index");
        }

        // =========================================================
        // LOAD ĐƠN VỊ
        // =========================================================
        private void LoadDepartments(
            int? selectedId = null)
        {
            ViewBag.DepartmentList =
                new SelectList(
                    db.departments
                        .OrderBy(d => d.name)
                        .ToList(),
                    "id",
                    "name",
                    selectedId
                );
        }

        // =========================================================
        // HIỂN THỊ TÊN ROLE
        // =========================================================
        private string GetRoleName(string role)
        {
            switch (role)
            {
                case "admin":
                    return "Quản trị viên";

                case "staff":
                    return "Cán bộ";

                case "student":
                    return "Sinh viên";

                default:
                    return role;
            }
        }

        // =========================================================
        // DISPOSE
        // =========================================================
        protected override void Dispose(
            bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}