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
                ),
                // --- PRODUSE NOI ---
                (
                    "Centura Piele Dark Iron Fitness",
                    "Conceputa pentru powerlifting si bodybuilding, din piele de bovina.\n\n- Piele naturala cu cusaturi duble ranforsate\n- Catarama heavy-duty cu doi pini\n- Latime uniforma pentru suport lombar maxim\n- Se muleaza pe corp inca de la prima utilizare",
                    "/images/Centura-Piele-Dark-Iron-Fitness.jpeg",
                    219.00m,
                    15,
                    "Echipamente",
                    ADMIN_ID
                ),
                (
                    "Chingi de Ridicare RDX",
                    "Chingi din bumbac dens cu insertii cauciucate, ideale pentru exercitii de tragere.\n\n- Bumbac rezistent la rupere\n- Captuseala neopren pentru incheieturi\n- Textura anti-alunecare\n- Lungime extinsa pentru prindere sigura pe bara",
                    "/images/Chingi-de-ridicare-RDX.jpeg",
                    49.00m,
                    50,
                    "Echipamente",
                    PROPOSER_ID
                ),
                (
                    "Centura pentru Dips cu Lant Gymreapers",
                    "Centura proiectata anatomic pentru flotari la paralele si tractiuni cu greutate.\n\n- Material textil durabil cu captuseala interioara moale\n- Lant de otel de 76 cm\n- Suporta incarcaturi de peste 100 kg\n- Lata in spate pentru distribuirea uniforma a greutatii",
                    "/images/Centura-pentru-Dips-cu-Lant.jpeg",
                    189.00m,
                    8,
                    "Echipamente",
                    ADMIN_ID
                ),
                (
                    "Pantaloni Scurti 2-in-1 Berserk Sacrifice",
                    "Piesa vestimentara hibrid ce combina stilul anime cu functionalitatea sportiva.\n\n- Exterior mesh usor si aerisit\n- Liner interior cu compresie usoara pe coapse\n- Buzunar ascuns pentru telefon\n- Grafica sublimata rezistenta la spalari",
                    "/images/Pantaloni-scurti-2-in-1.jpeg",
                    129.00m,
                    20,
                    "Accesorii & Imbracaminte",
                    PROPOSER_ID
                ),
                (
                    "Tricou Oversized Baki Hanma Acid Wash",
                    "Tricou definitia stilului Pump Cover, cu croiala larga si umeri cazuti.\n\n- 100% Bumbac Premium\n- Tratament Acid Wash pentru aspect vintage\n- Print grafic rezistent\n- Libertate totala de miscare",
                    "/images/Tricou-Oversized-Baki-Hanma.jpeg",
                    109.00m,
                    12,
                    "Accesorii & Imbracaminte",
                    ADMIN_ID
                ),
                (
                    "Colanti Seamless Mauve High Waist",
                    "Colanti fara cusaturi creati pentru incredere totala in sala.\n\n- Tehnologie Seamless, material dens si elastic\n- Talie inalta compresiva cu efect tummy control\n- Trece testul genuflexiunii (squat-proof)\n- Perforatii strategice pentru ventilatie",
                    "/images/Colanti-Seamless-Mauve.jpeg",
                    149.00m,
                    25,
                    "Accesorii & Imbracaminte",
                    PROPOSER_ID
                ),
                (
                    "Pantaloni Scurti Compresie Baki Face",
                    "Pantaloni scurti 2-in-1 inspirati de The Grappler, pentru luptatori si atleti.\n\n- Strat de baza mulat cu imprimeu manga detaliat\n- Strat superior scurt pentru mobilitate maxima\n- Buzunar spate cu fermoar\n- Material tehnic ultra-usor ce elimina rapid transpiratia",
                    "/images/Pantaloni-scurti-Compresie.jpeg",
                    135.00m,
                    18,
                    "Accesorii & Imbracaminte",
                    ADMIN_ID
                ),
                (
                    "Optimum Nutrition Gold Standard BCAA",
                    "Supliment esential pentru antrenamente pe stomacul gol sau sesiuni lungi.\n\n- 5 g BCAA per portie (Leucina, Izoleucina, Valina)\n- Magneziu si potasiu pentru electroliti\n- Zero zahar\n- Aroma racoritoare de Zmeura si Rodie",
                    "/images/Optimum-Nutrition-Gold-Standard-BCAA.jpeg",
                    139.00m,
                    40,
                    "Suplimente",
                    PROPOSER_ID
                ),
                (
                    "ABE Ultimate Pre-Workout Slush Puppie Edition",
                    "Pre-workout complet pentru energie, pompare si concentrare mentala.\n\n- 4 g Citrulina pentru vascularizare\n- 2 g Beta-Alanina pentru anduranta musculara\n- 200 mg Cofeina pentru focus\n- Aroma Slush Puppie, colaborare oficiala",
                    "/images/ABE-Ultimate-Pre-Workout-Slush-Puppie-Edition.jpeg",
                    159.00m,
                    10,
                    "Suplimente",
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
                ("Shaker Proteine din Otel Inoxidabil 750 ml", USER_ID, 5, "Il iau zilnic la sala si la birou, se spala usor si nu a curs niciodata.", DateTime.UtcNow.AddDays(-9)),

                // Centura Piele Dark Iron Fitness - 3 review-uri (ADMIN, PROPOSER, USER - cate unul)
                ("Centura Piele Dark Iron Fitness", ADMIN_ID, 5, "Excelenta pentru ridicari grele, suporta greutati mari fara sa se deformeze. Catarama e foarte rezistenta.", DateTime.UtcNow.AddDays(-20)),
                ("Centura Piele Dark Iron Fitness", PROPOSER_ID, 4, "O centura solida, ofera suport bun. Pielea e un pic rigida la inceput, dar se formeaza dupa cateva folosiri.", DateTime.UtcNow.AddDays(-17)),
                ("Centura Piele Dark Iron Fitness", USER_ID, 5, "Ideală pt. antrenamente intense. Nu aluneca si nu strangere prea tare. Recomand!", DateTime.UtcNow.AddDays(-13)),

                // Chingi de Ridicare RDX - 3 review-uri (ADMIN, PROPOSER, USER - cate unul)
                ("Chingi de Ridicare RDX", ADMIN_ID, 5, "Foarte utile pentru deadlift, ofera prindere excelenta. Buclele se regleaza usor.", DateTime.UtcNow.AddDays(-19)),
                ("Chingi de Ridicare RDX", PROPOSER_ID, 4, "Sunt eficiente, dar ar putea fi un pic mai lungi pentru maini mai mari. Materialul e de calitate.", DateTime.UtcNow.AddDays(-15)),
                ("Chingi de Ridicare RDX", USER_ID, 5, "Am observat o diferenta mare la ridicari. Nu se rup si nu provoaca vezicule ca alte chingi.", DateTime.UtcNow.AddDays(-10)),

                // Centura pentru Dips cu Lant Gymreapers - 3 review-uri (ADMIN, PROPOSER, USER - cate unul)
                ("Centura pentru Dips cu Lant Gymreapers", ADMIN_ID, 5, "Lantul e suficient de lung pentru toate variatiunile de dips. Centura se aseaza bine pe solduri.", DateTime.UtcNow.AddDays(-22)),
                ("Centura pentru Dips cu Lant Gymreapers", PROPOSER_ID, 4, "Foarte buna pentru antrenamentele la bara. Lantul ar putea fi usor mai greu, dar per total e robusta.", DateTime.UtcNow.AddDays(-16)),
                ("Centura pentru Dips cu Lant Gymreapers", USER_ID, 5, "Am reusit sa fac dips cu greutate pentru prima data, centura se comporta excelent. Nu aluneca deloc.", DateTime.UtcNow.AddDays(-11)),

                // Pantaloni Scurti 2-in-1 Berserk Sacrifice - 3 review-uri (ADMIN, PROPOSER, USER - cate unul)
                ("Pantaloni Scurti 2-in-1 Berserk Sacrifice", ADMIN_ID, 5, "Foarte confortabili si practici, materialul e de calitate. Imi plac buzunarele ascunse.", DateTime.UtcNow.AddDays(-21)),
                ("Pantaloni Scurti 2-in-1 Berserk Sacrifice", PROPOSER_ID, 4, "Sunt misto, dar ar trebui sa fie disponibili si pe negru. Imi place ca au doua lungimi.", DateTime.UtcNow.AddDays(-15)),
                ("Pantaloni Scurti 2-in-1 Berserk Sacrifice", USER_ID, 5, "Impecabili pentru antrenamentele de vara, materialul ventilat chiar ajuta. Nu se strang la talie.", DateTime.UtcNow.AddDays(-10)),

                // Tricou Oversized Baki Hanma Acid Wash - 3 review-uri (ADMIN, PROPOSER, USER - cate unul)
                ("Tricou-Oversized-Baki-Hanma Acid Wash", ADMIN_ID, 5, "Un tricou care atrage priviri, materialul e moale si placut pe corp. Croiala oversized e perfecta.", DateTime.UtcNow.AddDays(-18)),
                ("Tricou-Oversized-Baki-Hanma Acid Wash", PROPOSER_ID, 4, "Imi place designul, dar as fi vrut sa fie si pe culori mai inchise. 100% bumbac e un plus.", DateTime.UtcNow.AddDays(-14)),
                ("Tricou-Oversized-Baki-Hanma Acid Wash", USER_ID, 5, "Tricoul preferat pentru sala, permite o gamă larga de mișcări. Aspectul acid wash e foarte reusit.", DateTime.UtcNow.AddDays(-8)),

                // Colanti Seamless Mauve High Waist - 3 review-uri (ADMIN, PROPOSER, USER - cate unul)
                ("Colanti-Seamless-Mauve High Waist", ADMIN_ID, 5, "Colantii perfecti pentru antrenamentele intense, nu se degradeaza la spalare. Efectul tummy control functioneaza.", DateTime.UtcNow.AddDays(-17)),
                ("Colanti-Seamless-Mauve High Waist", PROPOSER_ID, 4, "Materialul e un pic mai gros decat ma asteptam, dar ofera suport bun. Modelul fara cusaturi e confortabil.", DateTime.UtcNow.AddDays(-13)),
                ("Colanti-Seamless-Mauve High Waist", USER_ID, 5, "Foarte multumita de achizitie, sunt grozavi pentru yoga si pilates. Culoarea mauve e eleganta si moderna.", DateTime.UtcNow.AddDays(-7))
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