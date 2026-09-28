using quanlySukien;
using quanlySukien.Filters;
using quanlySukien.Models;
using System;
using System.Linq;
using System.Web.Mvc;

namespace quanlySukien.Areas.Admin.Controllers
{
    [AdminAuthorize]
    public class CategoriesController : Controller
    {
        private event_managementEntities db =
            new event_managementEntities();

        // =========================================================
        // INDEX - DANH SÁCH DANH MỤC
        // =========================================================
        public ActionResult Index(string keyword)
        {
            var categories = db.event_categories.AsQueryable();

            // Tìm kiếm theo tên
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();

                categories = categories.Where(c =>
                    c.name.Contains(keyword));
            }

            var list = categories
                .OrderBy(c => c.name)
                .ToList();

            ViewBag.Keyword = keyword;

            // Thống kê
            ViewBag.TotalCategories =
                db.event_categories.Count();

            ViewBag.ActiveCategories =
                db.event_categories.Count(
                    c => c.status == "active");

            ViewBag.InactiveCategories =
                db.event_categories.Count(
                    c => c.status != "active");

            return View(list);
        }

        // =========================================================
        // GET: Admin/Categories/Create
        // =========================================================
        [HttpGet]
        public ActionResult Create()
        {
            return View();
        }

        // =========================================================
        // POST: Admin/Categories/Create
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(string name)
        {
            // Kiểm tra rỗng
            if (string.IsNullOrWhiteSpace(name))
            {
                ViewBag.Error =
                    "Vui lòng nhập tên loại sự kiện.";

                return View();
            }

            name = name.Trim();

            // Kiểm tra trùng tên
            bool exists =
                db.event_categories.Any(
                    c => c.name == name);

            if (exists)
            {
                ViewBag.Error =
                    "Loại sự kiện này đã tồn tại.";

                return View();
            }

            // Tạo danh mục mới
            var category = new event_categories
            {
                name = name,
                description = "",
                status = "active",
                created_at = DateTime.Now
            };

            db.event_categories.Add(category);

            db.SaveChanges();

            TempData["Success"] = "Thêm loại sự kiện thành công.";

            return RedirectToAction("Index");
        }

        // =========================================================
        // GET: Admin/Categories/Edit/5
        // =========================================================
        [HttpGet]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(400);
            }

            var category =
                db.event_categories.FirstOrDefault(
                    c => c.id == id.Value);

            if (category == null)
            {
                return HttpNotFound();
            }

            return View(category);
        }

        // =========================================================
        // POST: Admin/Categories/Edit
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(
            int id,
            string name)
        {
            var category =
                db.event_categories.FirstOrDefault(
                    c => c.id == id);

            if (category == null)
            {
                return HttpNotFound();
            }

            // Kiểm tra rỗng
            if (string.IsNullOrWhiteSpace(name))
            {
                ViewBag.Error =
                    "Vui lòng nhập tên loại sự kiện.";

                return View(category);
            }

            name = name.Trim();

            // Kiểm tra trùng
            bool exists =
                db.event_categories.Any(
                    c => c.name == name &&
                         c.id != id);

            if (exists)
            {
                ViewBag.Error =
                    "Loại sự kiện này đã tồn tại.";

                return View(category);
            }

            category.name = name;

            db.SaveChanges();

            TempData["Success"] =
                "Cập nhật loại sự kiện thành công.";

            return RedirectToAction("Index");
        }

        // =========================================================
        // POST: Admin/Categories/ToggleStatus
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleStatus(int id)
        {
            var category =
                db.event_categories.FirstOrDefault(
                    c => c.id == id);

            if (category == null)
            {
                return HttpNotFound();
            }

            if (category.status == "active")
            {
                category.status = "inactive";

                TempData["Success"] =
                    "Đã ẩn loại sự kiện. Staff sẽ không còn thấy loại này khi tạo sự kiện.";
            }
            else
            {
                category.status = "active";

                TempData["Success"] =
                    "Đã hiện lại loại sự kiện.";
            }

            db.SaveChanges();

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