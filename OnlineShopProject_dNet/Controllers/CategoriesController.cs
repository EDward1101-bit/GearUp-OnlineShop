using Microsoft.AspNetCore.Mvc;
using OnlineShopProject_dNet.Data;
using OnlineShopProject_dNet.Models;

namespace OnlineShopProject_dNet.Controllers
{
    public class CategoriesController(ApplicationDbContext context) : Controller
    {
        private readonly ApplicationDbContext db = context;

        public ActionResult Index()
        {
            var categories = from category in db.Categories
                             orderby category.Name
                             select category;
            ViewBag.Categories = categories;
            return View();
        }

        public ActionResult Show(int id)
        {
            Category category = db.Categories.Find(id);
            ViewBag.Category = category;
            return View();
        }

        public ActionResult New()
        {
            return View();
        }

        [HttpPost]
        public ActionResult New(Category cat)
        {
            try
            {
                db.Categories.Add(cat);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                return View();
            }
        }

        public ActionResult Edit(int id)
        {
            Category category = db.Categories.Find(id);
            ViewBag.Category = category;
            return View();
        }

        [HttpPost]
        public ActionResult Edit(int id, Category requestCategory)
        {
            try
            {
                Category category = db.Categories.Find(id);

                {
                    category.Name = requestCategory.Name;
                    db.SaveChanges();
                }

                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                ViewBag.Category = requestCategory;
                return View();
            }
        }

        [HttpPost]
        public ActionResult Delete(int id)
        {
            Category category = db.Categories.Find(id);
            db.Categories.Remove(category);
            db.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}
