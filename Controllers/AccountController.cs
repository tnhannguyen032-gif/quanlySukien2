using quanlySukien;
using quanlySukien.Models;
using System;
using System.Linq;
using System.Web.Mvc;

namespace quanlySukien.Controllers
{
    public class AccountController : Controller
    {
        private event_managementEntities db = new event_managementEntities();

        // ==========================================
        // GET: Account/Login
        // ==========================================
        public ActionResult Login()
        {
            return View();
        }


        // ==========================================
        // POST: Account/Login
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string username, string password)
        {
            // Kiểm tra dữ liệu đăng nhập
            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error =
                    "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.";

                return View();
            }


            // Tìm tài khoản
            var user = db.users.FirstOrDefault(x =>
                x.username == username &&
                x.password == password);


            // Không tìm thấy tài khoản
            if (user == null)
            {
                ViewBag.Error =
                    "Tên đăng nhập hoặc mật khẩu không đúng.";

                return View();
            }


            // Kiểm tra tài khoản có đang hoạt động không
            if (user.status != "active")
            {
                ViewBag.Error =
                    "Tài khoản của bạn đang bị khóa.";

                return View();
            }


            // ==========================================
            // LƯU THÔNG TIN ĐĂNG NHẬP
            // ==========================================

            Session["UserId"] = user.id;
            Session["Username"] = user.username;
            Session["FullName"] = user.full_name;
            Session["Role"] = user.role;


            // ==========================================
            // CHUYỂN HƯỚNG THEO QUYỀN
            // ==========================================

            // ADMIN
            if (user.role == "admin")
            {
                return RedirectToAction(
                    "Index",
                    "Dashboard",
                    new { area = "Admin" }
                );
            }


            // CÁN BỘ
            if (user.role == "staff")
            {
                return RedirectToAction(
                    "Index",
                    "StaffEvents",
                    new { area = "" }
                );
            }


            // SINH VIÊN
            if (user.role == "student")
            {
                return RedirectToAction(
                    "Index",
                    "Events",
                    new { area = "" }
                );
            }


            // Nếu role không xác định
            return RedirectToAction(
                "Index",
                "Home",
                new { area = "" }
            );
        }


        // ==========================================
        // GET: Account/Logout
        // ==========================================
        public ActionResult Logout()
        {
            Session.Clear();

            return RedirectToAction(
                "Login",
                "Account"
            );
        }


        // ==========================================
        // GET: Account/Register
        // ==========================================
        public ActionResult Register()
        {
            return View();
        }


        // ==========================================
        // POST: Account/Register
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(
            string username,
            string password,
            string confirmPassword,
            string full_name,
            string email,
            string phone,
            string student_code,
            string class_name,
            string major,
            string faculty)
        {
            // ==========================================
            // KIỂM TRA DỮ LIỆU BẮT BUỘC
            // ==========================================

            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(confirmPassword) ||
                string.IsNullOrWhiteSpace(full_name) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(student_code))
            {
                ViewBag.Error =
                    "Vui lòng nhập đầy đủ các thông tin bắt buộc.";

                return View();
            }


            // ==========================================
            // KIỂM TRA MẬT KHẨU
            // ==========================================

            if (password != confirmPassword)
            {
                ViewBag.Error =
                    "Mật khẩu xác nhận không khớp.";

                return View();
            }


            // ==========================================
            // KIỂM TRA USERNAME
            // ==========================================

            bool usernameExists =
                db.users.Any(x => x.username == username);

            if (usernameExists)
            {
                ViewBag.Error =
                    "Tên đăng nhập đã tồn tại.";

                return View();
            }


            // ==========================================
            // KIỂM TRA EMAIL
            // ==========================================

            bool emailExists =
                db.users.Any(x => x.email == email);

            if (emailExists)
            {
                ViewBag.Error =
                    "Email đã được sử dụng.";

                return View();
            }


            // ==========================================
            // KIỂM TRA MÃ SINH VIÊN
            // ==========================================

            bool studentCodeExists =
                db.student_profiles.Any(
                    x => x.student_code == student_code
                );

            if (studentCodeExists)
            {
                ViewBag.Error =
                    "Mã sinh viên đã tồn tại.";

                return View();
            }


            // ==========================================
            // TẠO TÀI KHOẢN
            // ==========================================

            var newUser = new user
            {
                username = username,
                password = password,
                full_name = full_name,
                email = email,
                phone = phone,

                // Tài khoản đăng ký trên web
                // luôn là sinh viên
                role = "student",

                department_id = null,
                status = "active",

                created_at = DateTime.Now
            };

            db.users.Add(newUser);
            db.SaveChanges();


            // ==========================================
            // TẠO HỒ SƠ SINH VIÊN
            // ==========================================

            var studentProfile = new student_profiles
            {
                user_id = newUser.id,
                student_code = student_code,
                class_name = class_name,
                major = major,
                faculty = faculty
            };

            db.student_profiles.Add(studentProfile);
            db.SaveChanges();


            // ==========================================
            // THÔNG BÁO ĐĂNG KÝ THÀNH CÔNG
            // ==========================================

            TempData["Success"] =
                "Đăng ký tài khoản thành công. Vui lòng đăng nhập.";


            return RedirectToAction("Login");
        }


        // ==========================================
        // Dispose
        // ==========================================
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