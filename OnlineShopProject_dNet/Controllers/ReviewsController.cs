using OnlineShopProject_dNet.Models;
using Microsoft.AspNetCore.Mvc;
using OnlineShopProject_dNet.Data;

namespace OnlineShopProject_dNet.Controllers
{
    public class ReviewsController(ApplicationDbContext context) : Controller
    {
        private readonly ApplicationDbContext db = context;
        
        //Adaugarea unui comentariu asociat unui produs in baza de date
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

        //Stergerea unui comentariu din baza de date(asociat unui produs)
        [HttpPost]
        public IActionResult Delete(int id)
        {
            Review rev = db.Comments.Find(id);
            db.Comments.Remove(rev);
            db.SaveChanges();
            return Redirect("/Product/Show/" + rev.ProductId);
        }

        // se editeaza un comentariu existent
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
    }
}
