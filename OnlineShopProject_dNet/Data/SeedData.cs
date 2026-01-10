using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OnlineShopProject_dNet.Models;

namespace OnlineShopProject_dNet.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>());

            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // --- CREAREA ROLURILOR ---
            string[] roleNames = { "Admin", "Proposer", "User" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // --- CREAREA UTILIZATORILOR ---
            const string ADMIN_ID = "9e445865-a24d-4543-a6c6-9443d048cdb9";
            const string PROPOSER_ID = "9e445865-a24d-4543-a6c6-9443d048cdb8";
            const string USER_ID = "9e445865-a24d-4543-a6c6-9443d048cdb7";
            const string ATHLETE_ID = "9e445865-a24d-4543-a6c6-9443d048cdb6";

            var userDefinitions = new List<(string Id, string Email, string First, string Last, string Password, string Role)>
            {
                (ADMIN_ID, "admin@test.com", "Admin", "Sistem", "Admin123!", "Admin"),
                (PROPOSER_ID, "proposer@test.com", "Dan", "Propunător", "Proposer123!", "Proposer"),
                (USER_ID, "user@test.com", "Ion", "Userescu", "User123!", "User"),
                (ATHLETE_ID, "andreea.fit@test.com", "Andreea", "Toma", "Andreea123!", "User")
            };

            var usersById = new Dictionary<string, ApplicationUser>();

            foreach (var definition in userDefinitions)
            {
                var existingUser = await userManager.FindByIdAsync(definition.Id);
                if (existingUser == null)
                {
                    var newUser = new ApplicationUser
                    {
                        Id = definition.Id,
                        UserName = definition.Email,
                        Email = definition.Email,
                        EmailConfirmed = true,
                        FirstName = definition.First,
                        LastName = definition.Last
                    };

                    var createResult = await userManager.CreateAsync(newUser, definition.Password);
                    if (createResult.Succeeded && !string.IsNullOrEmpty(definition.Role))
                    {
                        await userManager.AddToRoleAsync(newUser, definition.Role);
                    }

                    existingUser = newUser;
                }
                else if (!string.IsNullOrEmpty(definition.Role) && !await userManager.IsInRoleAsync(existingUser, definition.Role))
                {
                    await userManager.AddToRoleAsync(existingUser, definition.Role);
                }

                usersById[definition.Id] = existingUser;
            }

            // --- CATEGORII ---
            var categoryNames = new[]
            {
                "Suplimente",
                "Echipamente",
                "Accesorii & Îmbrăcăminte"
            };

            foreach (var name in categoryNames)
            {
                if (!context.Categories.Any(c => c.Name == name))
                {
                    context.Categories.Add(new Category { Name = name });
                }
            }

            await context.SaveChangesAsync();

            var categoriesByName = context.Categories.ToDictionary(c => c.Name, c => c);

            // --- PRODUSE ---
            var productDefinitions = new List<(string Title, string Description, string Image, decimal Price, int Stock, string CategoryName, string UserId)>
            {
                (
                    "Creatină Monohidrată Micronizată 500 g",
                    "Creatină monohidrată micronizată, testată pentru puritate, 100 de porții a câte 5 g. Se dizolvă ușor, fără arome artificiale. Perfectă pentru forță, volum și refacere rapidă după antrenamentele intense.",
                    "/images/creatina-monohidratata.jpeg",
                    149.90m,
                    120,
                    "Suplimente",
                    ADMIN_ID
                ),
                (
                    "Whey Protein Gold 1 kg",
                    "Proteina din zer premium cu 24 g proteină per porție și 5,5 g BCAA. Se mixează fin în shaker, gust echilibrat de vanilie și se absoarbe rapid pentru recuperare musculară și menținerea masei slabe.",
                    "/images/whey-protein-gold-1kg.jpg",
                    219.00m,
                    80,
                    "Suplimente",
                    PROPOSER_ID
                ),
                (
                    "Set Gantere Reglabile 20 kg",
                    "Set compact cu discuri metalice cauciucate și bare cu filet. Poți ajusta greutatea între 2 kg și 20 kg, mânere striate pentru grip sigur și inele de fixare cu filet care nu se slăbesc în timpul antrenamentului.",
                    "/images/set-gantere-reglabile.jpeg",
                    579.00m,
                    30,
                    "Echipamente",
                    ADMIN_ID
                ),
                (
                    "Geantă de Sală XL - Albă",
                    "Geantă spațioasă pentru sală, material impermeabil, compartiment ventilat pentru încălțăminte și buzunare laterale pentru shaker și accesorii mici. Curea reglabilă și întărituri pe fund pentru a rezista la greutate.",
                    "/images/gym-bag-white.jpeg",
                    189.00m,
                    45,
                    "Accesorii & Îmbrăcăminte",
                    PROPOSER_ID
                ),
                (
                    "Shaker Proteine din Oțel Inoxidabil 750 ml",
                    "Shaker izolat din oțel inoxidabil care nu prinde mirosuri, cu capac etanș și sită anti-cocoloașe. Păstrează băutura rece după antrenament și se curăță ușor în doar câteva clătiri.",
                    "/images/shaker-proteine-otel-inoxidabil.jpeg",
                    99.00m,
                    200,
                    "Accesorii & Îmbrăcăminte",
                    ADMIN_ID
                )
            };

            var productDefinitionsByTitle = productDefinitions.ToDictionary(p => p.Title, p => p);
            var productsByTitle = new Dictionary<string, Product>();

            foreach (var def in productDefinitions)
            {
                var product = context.Products
                    .Include(p => p.Reviews)
                    .FirstOrDefault(p => p.Title == def.Title);

                if (product == null)
                {
                    product = new Product
                    {
                        Title = def.Title,
                        Description = def.Description,
                        Image = def.Image,
                        Price = def.Price,
                        Stock = def.Stock,
                        CategoryId = categoriesByName[def.CategoryName].Id,
                        UserId = def.UserId,
                        Status = "Approved"
                    };
                    context.Products.Add(product);
                }
                else
                {
                    product.Status = "Approved";
                    product.CategoryId ??= categoriesByName[def.CategoryName].Id;
                    product.Image ??= def.Image;
                    product.Price ??= def.Price;
                    product.Stock ??= def.Stock;
                }

                productsByTitle[def.Title] = product;
            }

            await context.SaveChangesAsync();

            // --- COMENZI PLASATE (pentru a valida review-urile) ---
            var orderMetaByUser = new Dictionary<string, (string Address, DateTime Date)>
            {
                [ADMIN_ID] = ("Str. Sportivilor 10, București", DateTime.UtcNow.AddDays(-30)),
                [PROPOSER_ID] = ("Str. Atletismului 22, Cluj-Napoca", DateTime.UtcNow.AddDays(-24)),
                [USER_ID] = ("Bd. Independenței 15, Iași", DateTime.UtcNow.AddDays(-18)),
                [ATHLETE_ID] = ("Str. Energiei 7, Timișoara", DateTime.UtcNow.AddDays(-12))
            };

            var placedOrders = context.Orders
                .Include(o => o.OrderDetails)
                .Where(o => o.Status == "Placed" && o.UserId != null)
                .AsEnumerable()
                .GroupBy(o => o.UserId!)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(o => o.Date).First());

            foreach (var kvp in orderMetaByUser)
            {
                if (!placedOrders.TryGetValue(kvp.Key, out var order))
                {
                    order = new Order
                    {
                        UserId = kvp.Key,
                        Date = kvp.Value.Date,
                        Status = "Placed",
                        ShippingAddress = kvp.Value.Address,
                        TotalAmount = 0m
                    };
                    placedOrders[kvp.Key] = order;
                    context.Orders.Add(order);
                }
            }

            var purchases = new List<(string UserId, string ProductTitle, int Quantity)>
            {
                (ADMIN_ID, "Creatină Monohidrată Micronizată 500 g", 1),
                (ADMIN_ID, "Set Gantere Reglabile 20 kg", 1),
                (ADMIN_ID, "Geantă de Sală XL - Albă", 1),

                (PROPOSER_ID, "Whey Protein Gold 1 kg", 1),
                (PROPOSER_ID, "Set Gantere Reglabile 20 kg", 1),
                (PROPOSER_ID, "Geantă de Sală XL - Albă", 1),
                (PROPOSER_ID, "Shaker Proteine din Oțel Inoxidabil 750 ml", 1),

                (USER_ID, "Creatină Monohidrată Micronizată 500 g", 1),
                (USER_ID, "Whey Protein Gold 1 kg", 1),
                (USER_ID, "Geantă de Sală XL - Albă", 1),
                (USER_ID, "Shaker Proteine din Oțel Inoxidabil 750 ml", 1),

                (ATHLETE_ID, "Creatină Monohidrată Micronizată 500 g", 1),
                (ATHLETE_ID, "Whey Protein Gold 1 kg", 1),
                (ATHLETE_ID, "Set Gantere Reglabile 20 kg", 1),
                (ATHLETE_ID, "Shaker Proteine din Oțel Inoxidabil 750 ml", 1)
            };

            foreach (var purchase in purchases)
            {
                if (!productsByTitle.TryGetValue(purchase.ProductTitle, out var product))
                {
                    continue;
                }

                var def = productDefinitionsByTitle[purchase.ProductTitle];
                var order = placedOrders[purchase.UserId];

                var hasDetail = order.OrderDetails.Any(od => od.ProductId == product.Id);
                if (!hasDetail)
                {
                    order.OrderDetails.Add(new OrderDetail
                    {
                        ProductId = product.Id,
                        ProductTitleSnapshot = product.Title,
                        ProductImageSnapshot = product.Image,
                        ProductCategorySnapshot = def.CategoryName,
                        Quantity = purchase.Quantity,
                        UnitPrice = product.Price ?? def.Price,
                        Order = order
                    });
                }
            }

            foreach (var order in placedOrders.Values)
            {
                order.TotalAmount = order.OrderDetails.Sum(od => od.UnitPrice * od.Quantity);
            }

            await context.SaveChangesAsync();

            // --- REVIEW-URI ---
            var reviewSeeds = new List<(string ProductTitle, string UserId, int Rating, string Content, DateTime Date)>
            {
                ("Creatină Monohidrată Micronizată 500 g", ADMIN_ID, 5, "Se dizolvă perfect în apă rece și nu are gust nisipos. Am simțit mai multă energie la seriile grele după 2 săptămâni.", DateTime.UtcNow.AddDays(-20)),
                ("Creatină Monohidrată Micronizată 500 g", USER_ID, 4, "O folosesc după antrenamente și recuperarea e mai rapidă. Mi-ar fi plăcut să includă și o linguriță gradată în pachet.", DateTime.UtcNow.AddDays(-16)),
                ("Creatină Monohidrată Micronizată 500 g", ATHLETE_ID, 5, "Micronizată real, nu face spumă și nu deranjează digestia. Ideală pentru ciclurile de volum.", DateTime.UtcNow.AddDays(-14)),

                ("Whey Protein Gold 1 kg", PROPOSER_ID, 5, "Gust de vanilie echilibrat, se mixează fin în shaker și nu face cocoloașe. 24 g de proteină per porție, excelent după antrenamente.", DateTime.UtcNow.AddDays(-18)),
                ("Whey Protein Gold 1 kg", USER_ID, 4, "Îl folosesc dimineața cu ovăz, nu balonează și se digeră ușor. Ar fi grozav să existe și variantă de 2 kg.", DateTime.UtcNow.AddDays(-15)),
                ("Whey Protein Gold 1 kg", ATHLETE_ID, 5, "Proteina e curată, fără arome artificiale tari. Am observat recuperare mai bună la antrenamentele de forță.", DateTime.UtcNow.AddDays(-12)),

                ("Set Gantere Reglabile 20 kg", ADMIN_ID, 5, "Mânerele striate prind bine și schimb greutățile rapid. Discurile cauciucate nu zgârie podeaua și nu fac zgomot mare.", DateTime.UtcNow.AddDays(-22)),
                ("Set Gantere Reglabile 20 kg", PROPOSER_ID, 4, "Sistemul de prindere e sigur, dar șuruburile trebuie strânse din când în când. Ideal pentru antrenamente acasă.", DateTime.UtcNow.AddDays(-17)),
                ("Set Gantere Reglabile 20 kg", ATHLETE_ID, 5, "Set compact, ocupă puțin spațiu și am reușit să înlocuiesc complet vechile gantere fixe.", DateTime.UtcNow.AddDays(-11)),

                ("Geantă de Sală XL - Albă", ADMIN_ID, 5, "Încape tot: încălțăminte, prosop, centură și shaker. Compartimentul ventilat pentru pantofi chiar își face treaba.", DateTime.UtcNow.AddDays(-19)),
                ("Geantă de Sală XL - Albă", PROPOSER_ID, 4, "Material gros și fermoare solide. Mi-ar fi plăcut o curea de umăr puțin mai lată pentru drumuri lungi.", DateTime.UtcNow.AddDays(-13)),
                ("Geantă de Sală XL - Albă", USER_ID, 5, "Buzunarele pentru chei și telefon sunt exact ce aveam nevoie. Își păstrează forma chiar și încărcată.", DateTime.UtcNow.AddDays(-10)),

                ("Shaker Proteine din Oțel Inoxidabil 750 ml", PROPOSER_ID, 5, "Capacul etanș nu curge deloc, iar oțelul nu prinde mirosuri. Ideal după antrenamentele de seară.", DateTime.UtcNow.AddDays(-21)),
                ("Shaker Proteine din Oțel Inoxidabil 750 ml", USER_ID, 4, "Păstrează shake-ul rece mai mult timp și sita anti-cocoloașe chiar funcționează. E un pic mai greu, dar foarte solid.", DateTime.UtcNow.AddDays(-12)),
                ("Shaker Proteine din Oțel Inoxidabil 750 ml", ATHLETE_ID, 5, "Îl iau zilnic la sală și la birou, se spală ușor și nu a curs niciodată. Simt că o să mă țină ani buni.", DateTime.UtcNow.AddDays(-9))
            };

            foreach (var reviewSeed in reviewSeeds)
            {
                if (!productsByTitle.TryGetValue(reviewSeed.ProductTitle, out var product))
                {
                    continue;
                }

                var alreadyExists = context.Reviews.Any(r => r.ProductId == product.Id && r.UserId == reviewSeed.UserId);
                if (!alreadyExists)
                {
                    context.Reviews.Add(new Review
                    {
                        ProductId = product.Id,
                        UserId = reviewSeed.UserId,
                        Rating = reviewSeed.Rating,
                        Content = reviewSeed.Content,
                        Date = reviewSeed.Date
                    });
                }
            }

            await context.SaveChangesAsync();

            // --- CALCUL MEDII RATING ---
            foreach (var product in productsByTitle.Values)
            {
                var ratings = context.Reviews
                    .Where(r => r.ProductId == product.Id && r.Rating.HasValue)
                    .Select(r => r.Rating!.Value)
                    .ToList();

                product.Rating = ratings.Any() ? (float)ratings.Average() : null;
            }

            await context.SaveChangesAsync();
        }
    }
}