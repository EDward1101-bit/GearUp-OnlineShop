# OnlineShopProject_dNet – ASP.NET Core MVC Online Shop

Important (configura?ie necesar? înainte de rulare)
- Proiectul necesit? un fi?ier de configurare valid; altfel aplica?ia nu porne?te.
- Crea?i un fi?ier `OnlineShopProject_dNet/appsettings.json` (sau redenumi?i `OnlineShopProject_dNet/appsettings.example.json` în `appsettings.json`) ?i completa?i:
  - `ConnectionStrings:DefaultConnection` – conexiunea la SQL Server
  - `GoogleAI:ApiKey` – cheia pentru Google Gemini (Generative Language API)
- Recomandat: NU comite?i `appsettings.json` în Git. Alternativ, folosi?i Secret Manager: `dotnet user-secrets`.

Rezumat
Aplica?ie web tip magazin online, construit? în `ASP.NET Core MVC` (net9.0) ?i `Entity Framework Core`, cu `ASP.NET Identity` pentru autentificare ?i roluri. Include flux complet de produse (CRUD, propuneri/aprobat), categorii dinamice, co?, comenzi, wishlist, c?utare/filtrare/sortare, review-uri cu rating ?i un asistent AI pentru produse bazat pe `Google Gemini`.

Tehnologii ?i decizii cheie
- Backend: `ASP.NET Core MVC` (net9.0), `EF Core` (SQL Server)
- Identitate ?i roluri: `ASP.NET Identity` (Admin, Proposer, User, plus vizitatori neautentifica?i)
- UI: `Razor Views`, `Bootstrap`
- AI: `Google Gemini` prin `GoogleProductAiService` (HTTP API), prompt strict cu fallback controlat
- Securitate: valid?ri server-side, filtru global Anti-Forgery, înc?rcare imagini cu whitelist extensii ?i limit? m?rime, sanitizare HTML (`HtmlSanitizer`)
- Observabilitate: logging (Console/Debug), mesaje TempData pentru feedback UX
- Persisten??: rela?ii configurate explicit, cascade delete, chei compuse (wishlist), snapshot-uri în `OrderDetail` pentru istoric

Func?ionalit??i implementate (mapare pe cerin?e)
1) Tipuri de utilizatori ?i roluri
- Vizitator: poate vizualiza produse/review-uri; la ac?iuni restric?ionate este redirec?ionat la login
- User: co?, comenzi, wishlist, review-uri
- Proposer: propune produse; poate edita/?terge DOAR produsele proprii (cu reguli de status)
- Admin: gestioneaz? categorii, produse, review-uri, utilizatori; decide aprob?ri/respingeri

2) Categorii dinamice
- CRUD din interfa??; nume unic ?i obligatoriu
- ?tergerea unei categorii ?terge toate produsele asociate (cascade delete)
- Afi?are în meniu ?i filtrare dup? categorie

3) Produse – ad?ugare ?i gestionare
- Câmpuri: titlu, descriere, imagine, pre?, stoc, rating (1–5), review-uri
- Valid?ri: pre? > 0, stoc ? 0; imagine cu extensii permise ?i max 5MB; sanitizare HTML pentru descriere
- Rating calculat ca medie a review-urilor (op?ional)
- Imagini salvate în `wwwroot/images` cu fallback implicit

4) Flux colaborator (propuneri ?i re-aprob?ri)
- Propunerile intr? în status „Pending” ?i necesit? decizie Admin („Approved”/„Rejected”)
- Proposer poate edita/?terge doar con?inut propriu; la editare produsul revine în „Pending”
- Feedback c?tre autor prin sistemul de notific?ri

5) Vizitator ?i acces restric?ionat
- La încercarea de a ad?uga în co?/wishlist, vizitatorul este direc?ionat la autentificare cu mesaj informativ

6) Co?, comenzi ?i wishlist
- Co? per utilizator, linii de comand? cu `UnitPrice` ?i snapshot detalii produs
- Validare stoc la fiecare ac?iune; decrement stoc la plasare comand?
- Wishlist per utilizator, f?r? duplicate (cheie compus? `UserId, ProductId`)
- Mutare rapid? din wishlist în co?

7) Review-uri ?i rating
- User-ul adaug?/editeaz?/?terge review-uri (text op?ional, rating op?ional 1–5)
- Recalcul automat al scorului la opera?ii pe review-uri
- Op?ional: restric?ie practic? – review doar dac? produsul a fost cump?rat

8) C?utare, filtrare, sortare
- C?utare par?ial? în titlu („lapto” ? „laptop”)
- Filtrare pe categorie, sortare dup? pre?/rating/nume (asc/desc), paginare

9) Component? AI – „Product Assistant” (Google Gemini)
- Chat lateral per produs cu r?spunsuri din descriere ?i `FAQ`
- Prompt strict în limba român?; f?r? halucina?ii; r?spuns fallback: „Momentan nu avem detalii despre acest aspect.”
- Salvare/folosire `FAQ` generale ?i specifice produsului

10) Administrare platform?
- Admin poate aproba/respinge produse, gestiona categorii, produse, review-uri, utilizatori

11) Calitatea proiectului
- Organizare MVC clar? (`Models`, `Views`, `Controllers`), servicii pentru logic? de domeniu (`Services`)
- Valid?ri, mesaje de eroare clare, seed de date de baz? (utilizatori/roluri)
- Documenta?ie ?i instruc?iuni de rulare

Arhitectur? (scurt)
- `Controllers`: `ProductsController`, `OrdersController`, `ReviewsController`, `WishlistController`, `AdminController`, `ProductAIController` etc.
- `Services`: `GoogleProductAiService` (integrare Gemini), `ProductAIService` (fallback/FAQ), `CartService`, `NotificationService`, `TextProcessingService`, `HtmlSanitizationService`
- `Data`: `ApplicationDbContext` (EF Core), `SeedData`
- `Models`: `Product`, `Category`, `Review`, `FAQ`, `Order`, `OrderDetail`, `Wishlist`, `Notification`, `ApplicationUser`

Configurare ?i rulare (Quick start)
Prerechizite
- .NET SDK 9.0
- SQL Server (ex. LocalDB) sau o instan?? SQL accesibil?
- Cheie API Google Gemini (Generative Language API)

1) Configura?i set?rile aplica?iei
- Copia?i `OnlineShopProject_dNet/appsettings.example.json` în `OnlineShopProject_dNet/appsettings.json` ?i actualiza?i:
```
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=OnlineShop;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  "GoogleAI": {
    "ApiKey": "<CHEIA_VOASTR?_GEMINI>"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```
- Alternativ, stoca?i cheia cu Secret Manager (în directorul proiectului `OnlineShopProject_dNet`):
```
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<conn_string>"
dotnet user-secrets set "GoogleAI:ApiKey" "<cheie_gemini>"
```

2) Restaura?i pachetele ?i aplica?i migra?iile
În directorul `OnlineShopProject_dNet/`:
```
dotnet restore
# Necesit? EF Core Tools:  dotnet tool update --global dotnet-ef
# Apoi:
dotnet ef database update
```

3) Rula?i aplica?ia
```
dotnet run
```
Aplica?ia porne?te pe https://localhost:xxxx. La prima rulare sunt create rolurile ?i utilizatorii demo prin `SeedData`.

Utilizatori demo (Seed)
- Admin: `admin@test.com` / `Admin123!`
- Proposer: `proposer@test.com` / `Proposer123!`
- User: `user@test.com` / `User123!`
Not?: parolele ?i conturile sunt pentru dezvoltare. Modifica?i în produc?ie.

Detalii integrare AI (Gemini)
- Serviciu: `GoogleProductAiService` apeleaz? Google Gemini (model: `gemini-2.5-flash`) prin endpointul Generative Language API
- Input: descriere produs + categorie + stoc + pre? + `FAQ` relevante + întrebarea utilizatorului
- Politic? r?spuns: numai în limitele contextului; fallback clar („Momentan nu avem detalii despre acest aspect.”)
- Configurare: cheia în `GoogleAI:ApiKey`; f?r? cheie valid?, componenta AI r?spunde cu fallback ?i logheaz? avertisment

Baz? de date ?i reguli model
- `Category` 1–N `Product` (cascade delete)
- `Product` 1–N `Review` (cascade delete)
- `Order` 1–N `OrderDetail` (cascade delete); `OrderDetail` ? `Product` cu `SetNull` ?i snapshot câmpuri pentru istoric
- `Wishlist` PK compus (`UserId`, `ProductId`) – f?r? duplicate
- `Notification` 1–N `ApplicationUser`

Securitate ?i calitatea datelor
- Filtru global Anti-Forgery pe `Controllers`
- Sanitizare HTML ?i text pe descrieri/review-uri (`HtmlSanitizer`)
- Valid?ri stricte înc?rcare imagini (format ?i dimensiune)
- Autorizare pe ac?iuni critice: Admin/Proposer pentru workflow produse; User pentru ac?iuni de cump?rare/review

Observa?ii
- Proiectul este configurat pentru SQL Server (DefaultConnection). Pachetul SQLite este inclus dar nu este folosit în configurarea implicit?.
- `appsettings.json` este obligatoriu. F?r? acesta sau f?r? valori valide pentru `DefaultConnection` ?i `GoogleAI:ApiKey`, aplica?ia va e?ua la pornire.
- Nu înc?rca?i chei secrete în repository.

Structur? relevant?
- `OnlineShopProject_dNet/Program.cs` – configurare servicii, pipeline, seeding
- `OnlineShopProject_dNet/Data/ApplicationDbContext.cs` – modele, rela?ii, reguli EF Core
- `OnlineShopProject_dNet/Controllers/*.cs` – logica MVC (produse, comenzi, review, AI, admin)
- `OnlineShopProject_dNet/Services/*.cs` – servicii de domeniu (AI Gemini, co?, notific?ri, sanitizare)
- `OnlineShopProject_dNet/Views/*` – interfa?? (Razor + Bootstrap)

Licen??
Acest proiect este pentru uz educa?ional/demonstrativ. Verifica?i politicile interne înainte de utilizare în produc?ie.