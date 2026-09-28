using quanlySukien;
using quanlySukien.Filters;
using quanlySukien.Models;
using System;
using System.Linq;
using System.Web.Mvc;

namespace quanlySukien.Areas.Admin.Controllers
{
    [AdminAuthorize]
    public class DepartmentsController : Controller
    {
        private event_managementEntities db =
            new event_managementEntities();

        // =========================================================
        // INDEX - DANH SÁCH ĐƠN VỊ
        // =========================================================
        public ActionResult Index(string keyword)
        {
            var departments = db.departments.AsQueryable();

            // Tìm kiếm
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();

                departments = departments.Where(d =>
                    d.name.Contains(keyword));
            }

            var list = departments
                .OrderBy(d => d.name)
                .ToList();

            ViewBag.Keyword = keyword;

            ViewBag.TotalDepartments =
                db.departments.Count();

            return View(list);
        }

        // =========================================================
        // GET: Admin/Departments/Create
        // =========================================================
        [HttpGet]
        public ActionResult Create()
        {
            return View();
        }

        // =========================================================
        // POST: Admin/Departments/Create
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(string name)
        {
            // Kiểm tra rỗng
            if (string.IsNullOrWhiteSpace(name))
            {
                ViewBag.Error =
                    "Vui lòng nhập tên đơn vị.";

                return View();
            }

            name = name.Trim();

            // Kiểm tra trùng tên
            bool exists =
                db.departments.Any(d =>
                    d.name == name);

            if (exists)
            {
                ViewBag.Error =
                    "Tên đơn vị đã tồn tại.";

                return View();
            }

            // Tạo đơn vị mới
            var department = new department
            {
                name = name,
                description = "",
                email = "",
                phone = "",
                status = "active",
                created_at = DateTime.Now
            };

            db.departments.Add(department);

            db.SaveChanges();

            TempData["Success"] = "Thêm đơn vị thành công.";

            return RedirectToAction("Index");
        }

        // =========================================================
        // GET: Admin/Departments/Edit/5
        // =========================================================
        [HttpGet]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(400);
            }

            var department =
                db.departments.FirstOrDefault(
                    d => d.id == id.Value);

            if (department == null)
            {
                return HttpNotFound();
            }

            return View(department);
        }

        // =========================================================
        // POST: Admin/Departments/Edit
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(
            int id,
            string name)
        {
            var department =
                db.departments.FirstOrDefault(
                    d => d.id == id);

            if (department == null)
            {
                return HttpNotFound();
            }

            // Kiểm tra rỗng
            if (string.IsNullOrWhiteSpace(name))
            {
                ViewBag.Error =
                    "Vui lòng nhập tên đơn vị.";

                return View(department);
            }

            name = name.Trim();

            // Kiểm tra trùng với đơn vị khác
            bool exists =
                db.departments.Any(d =>
                    d.name == name &&
                    d.id != id);

            if (exists)
            {
                ViewBag.Error =
                    "Tên đơn vị đã tồn tại.";

                return View(department);
            }

            // Cập nhật
            department.name = name;

            db.SaveChanges();

            TempData["Success"] =
                "Cập nhật đơn vị thành công.";

            return RedirectToAction("Index");
        }

        // =========================================================
        // DISPOSE
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