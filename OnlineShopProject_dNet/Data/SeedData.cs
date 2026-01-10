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

            var userDefinitions = new List<(string Id, string Email, string First, string Last, string Password, string Role)>
            {
                (ADMIN_ID, "admin@test.com", "Admin", "Sistem", "Admin123!", "Admin"),
                (PROPOSER_ID, "proposer@test.com", "Dan", "Propunator", "Proposer123!", "Proposer"),
                (USER_ID, "user@test.com", "Ion", "Userescu", "User123!", "User")
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
                "Accesorii & Imbracaminte"
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
                    "Creatina Monohidratata Micronizata 500 g",
                    "Creatina monohidratata micronizata, testata pentru puritate.\n\n- 100 de portii a cate 5 g\n- Se dizolva rapid, fara gust nisipos\n- Perfecta pentru forta, volum si refacere dupa antrenamentele grele",
                    "/images/creatina-monohidratata.jpeg",
                    149.90m,
                    120,
                    "Suplimente",
                    ADMIN_ID
                ),
                (
                    "Whey Protein Gold 1 kg",
                    "Proteina din zer premium, gust fin de vanilie.\n\n- 24 g proteina + 5,5 g BCAA per portie\n- Se amesteca perfect in shaker, fara cocoloase\n- Absorbtie rapida pentru recuperare si mentinerea masei slabe",
                    "/images/whey-protein-gold-1kg.jpg",
                    219.00m,
                    80,
                    "Suplimente",
                    PROPOSER_ID
                ),
                (
                    "Set Gantere Reglabile 20 kg",
                    "Set compact cu discuri metalice cauciucate si bare cu filet.\n\n- Ajustare intre 2 kg si 20 kg\n- Manere striate pentru grip sigur\n- Inele de fixare cu filet stabile chiar si la superserii",
                    "/images/set-gantere-reglabile.jpeg",
                    579.00m,
                    30,
                    "Echipamente",
                    ADMIN_ID
                ),
                (
                    "Geanta de Sala XL - Alba",
                    "Geanta spatioasa pentru sala, material impermeabil.\n\n- Compartiment ventilat pentru incaltaminte\n- Buzunare laterale pentru shaker si accesorii\n- Curea reglabila, intarituri pe fund pentru greutate",
                    "/images/gym-bag-white.jpeg",
                    189.00m,
                    45,
                    "Accesorii & Imbracaminte",
                    PROPOSER_ID
                ),
                (
                    "Shaker Proteine din Otel Inoxidabil 750 ml",
                    "Shaker izolat din otel inoxidabil, nu prinde mirosuri.\n\n- Capac etans si sita anti-cocoloase\n- Pastreaza bautura rece dupa antrenament\n- Se curata usor si rezista la uz zilnic",
                    "/images/shaker-proteine-otel-inoxidabil.jpeg",
                    99.00m,
                    200,
                    "Accesorii & Imbracaminte",
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
                [ADMIN_ID] = ("Str. Sportivilor 10, Bucuresti", DateTime.UtcNow.AddDays(-30)),
                [PROPOSER_ID] = ("Str. Atletismului 22, Cluj-Napoca", DateTime.UtcNow.AddDays(-24)),
                [USER_ID] = ("Bd. Independentei 15, Iasi", DateTime.UtcNow.AddDays(-18))
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
                (ADMIN_ID, "Creatina Monohidratata Micronizata 500 g", 1),
                (ADMIN_ID, "Set Gantere Reglabile 20 kg", 1),
                (ADMIN_ID, "Geanta de Sala XL - Alba", 1),
                (ADMIN_ID, "Shaker Proteine din Otel Inoxidabil 750 ml", 1),

                (PROPOSER_ID, "Whey Protein Gold 1 kg", 1),
                (PROPOSER_ID, "Set Gantere Reglabile 20 kg", 1),
                (PROPOSER_ID, "Geanta de Sala XL - Alba", 1),
                (PROPOSER_ID, "Shaker Proteine din Otel Inoxidabil 750 ml", 1),

                (USER_ID, "Creatina Monohidratata Micronizata 500 g", 1),
                (USER_ID, "Whey Protein Gold 1 kg", 1),
                (USER_ID, "Geanta de Sala XL - Alba", 1),
                (USER_ID, "Shaker Proteine din Otel Inoxidabil 750 ml", 1)
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

            // --- REVIEW-URI (fiecare utilizator poate lasa maxim 1 review per produs) ---
            // Regula: un user = un review per produs, review-uri variate si realiste
            var reviewSeeds = new List<(string ProductTitle, string UserId, int Rating, string Content, DateTime Date)>
            {
                // Creatina Monohidratata - 3 review-uri (ADMIN, PROPOSER, USER - cate unul)
                ("Creatina Monohidratata Micronizata 500 g", ADMIN_ID, 5, "Se dizolva perfect in apa rece si nu are gust nisipos. Am simtit mai multa energie la seriile grele dupa doua saptamani.", DateTime.UtcNow.AddDays(-20)),
                ("Creatina Monohidratata Micronizata 500 g", PROPOSER_ID, 4, "Micronizata fin, nu baloneaza. O iau cu suc de portocale si intru mai repede in antrenament.", DateTime.UtcNow.AddDays(-18)),
                ("Creatina Monohidratata Micronizata 500 g", USER_ID, 1, "Produsul a ajuns desfacut", DateTime.UtcNow.AddDays(-15)),

                // Whey Protein Gold - 3 review-uri (ADMIN, PROPOSER, USER - cate unul)
                ("Whey Protein Gold 1 kg", ADMIN_ID, 5, "Gust echilibrat de vanilie, se mixeaza fin in shaker si nu face spuma. 24 g proteina per portie, excelent post-antrenament.", DateTime.UtcNow.AddDays(-19)),
                ("Whey Protein Gold 1 kg", PROPOSER_ID, 2, "Gust mediocru", DateTime.UtcNow.AddDays(-16)),
                ("Whey Protein Gold 1 kg", USER_ID, 4, "Proteina e curata, fara arome artificiale tari. M-a ajutat la recuperare dupa antrenamentele de forta.", DateTime.UtcNow.AddDays(-12)),

                // Set Gantere Reglabile - 3 review-uri (ADMIN, PROPOSER, USER - cate unul)
                ("Set Gantere Reglabile 20 kg", ADMIN_ID, 5, "Manerele striate prind bine si schimb greutatile rapid. Discurile cauciucate nu zgarie podeaua si nu fac zgomot.", DateTime.UtcNow.AddDays(-22)),
                ("Set Gantere Reglabile 20 kg", PROPOSER_ID, 3, "Sistemul de prindere e sigur, dar suruburile trebuie stranse periodic. Ok pentru antrenamente acasa.", DateTime.UtcNow.AddDays(-17)),
                ("Set Gantere Reglabile 20 kg", USER_ID, 5, "Compacte, ocupa putin spatiu. Am inlocuit vechile gantere fixe fara sa simt diferenta la grip.", DateTime.UtcNow.AddDays(-11)),

                // Geanta de Sala XL - 3 review-uri (ADMIN, PROPOSER, USER - cate unul)
                ("Geanta de Sala XL - Alba", ADMIN_ID, 4, "Incape tot: pantofi, prosop, centura, shaker. Compartimentul ventilat chiar isi face treaba.", DateTime.UtcNow.AddDays(-19)),
                ("Geanta de Sala XL - Alba", PROPOSER_ID, 5, "Material gros, fermoare solide. As fi vrut curea de umar mai lata, dar per total e foarte buna.", DateTime.UtcNow.AddDays(-13)),
                ("Geanta de Sala XL - Alba", USER_ID, 4, "Buzunare bine gandite pentru telefon si chei. Isi pastreaza forma chiar si plina.", DateTime.UtcNow.AddDays(-10)),

                // Shaker Proteine - 3 review-uri (ADMIN, PROPOSER, USER - cate unul)
                ("Shaker Proteine din Otel Inoxidabil 750 ml", ADMIN_ID, 5, "Capacul etans nu curge deloc, otelul nu prinde mirosuri. Ideal dupa antrenamentele de seara.", DateTime.UtcNow.AddDays(-21)),
                ("Shaker Proteine din Otel Inoxidabil 750 ml", PROPOSER_ID, 4, "Pastreaza shake-ul rece mai mult timp. E un pic mai greu decat plasticul, dar solid si durabil.", DateTime.UtcNow.AddDays(-14)),
                ("Shaker Proteine din Otel Inoxidabil 750 ml", USER_ID, 5, "Il iau zilnic la sala si la birou, se spala usor si nu a curs niciodata.", DateTime.UtcNow.AddDays(-9))
            };

            foreach (var reviewSeed in reviewSeeds)
            {
                if (!productsByTitle.TryGetValue(reviewSeed.ProductTitle, out var product))
                {
                    continue;
                }

                // Verificam ca nu exista deja un review de la acest user pentru acest produs
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