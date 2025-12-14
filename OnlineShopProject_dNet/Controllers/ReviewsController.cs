using OnlineShopProject_dNet.Models;
using Microsoft.AspNetCore.Mvc;
using OnlineShopProject_dNet.Data;

namespace OnlineShopProject_dNet.Controllers
{
    public class ReviewsController(ApplicationDbContext context) : Controller
    {
        private readonly ApplicationDbContext db = context;
        
        //Adaugarea unui review asociat unui produs in baza de date
        [HttpPost]
        public IActionResult New(Review rev)
        {
            rev.Date = DateTime.Now;

            try
            {
                db.Reviews.Add(rev);
                db.SaveChanges();
                return Redirect("/Product/Show" + rev.ProductId);
            }
            catch (Exception)
            {
                return Redirect("/Product/Show" + rev.ProductId);
            }
        }

        //Stergerea unui review din baza de date(asociat unui produs)
        [HttpPost]
        public IActionResult Delete(int id)
        {
            Review rev = db.Reviews.Find(id);
            db.Reviews.Remove(rev);
            db.SaveChanges();
            return Redirect("/Product/Show/" + rev.ProductId);
        }

        // In acest moment vom implementa editarea intr-o pagina View separata
        // Se editeaza un comentariu existent
        // [HttpGet] implicit
        public IActionResult Edit(int id)
        {
            Review rev = db.Reviews.Find(id);
            ViewBag.Review = rev;
            return View();
        }

        [HttpPost]
        public IActionResult Edit(int id, Review requestReview)
        {
            Review rev = db.Reviews.Find(id);
            try
            {
                rev.Text = requestReview.Text;

                db.SaveChanges();

                return Redirect("/Product/Show/" + rev.ProductId);
            }
            catch (Exception)
            {
                return Redirect("/Product/Show/" + rev.ProductId);
            }
        }

        //TODO: Update rating al unui review
        //[HttpPost]

    }
}
