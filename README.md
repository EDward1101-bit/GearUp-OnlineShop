# OnlineShopProject_dNet

<div align="center">

**ASP.NET Core MVC Online Shop Platform**

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=flat&logo=dotnet)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-12.0-239120?style=flat&logo=csharp)](https://learn.microsoft.com/dotnet/csharp/)
[![Entity Framework](https://img.shields.io/badge/EF%20Core-9.0-512BD4?style=flat)](https://docs.microsoft.com/ef/core/)
[![ASP.NET Identity](https://img.shields.io/badge/ASP.NET%20Identity-9.0-512BD4?style=flat)](https://learn.microsoft.com/aspnet/core/security/authentication/identity)
[![SQL Server](https://img.shields.io/badge/SQL%20Server-LocalDB-CC2927?style=flat&logo=microsoftsqlserver)](https://www.microsoft.com/sql-server)
[![Google Gemini](https://img.shields.io/badge/Google%20Gemini-AI-4285F4?style=flat&logo=google)](https://ai.google.dev/)

</div>

---

## ?? Configura?ie Necesar? (Înainte de Rulare)

> **Proiectul nu va porni f?r? configurarea corect? a fi?ierului `appsettings.json`**

**Pa?ii obligatorii:**

1. Crea?i fi?ierul `OnlineShopProject_dNet/appsettings.json` sau redenumi?i `appsettings.example.json`
2. Completa?i valorile necesare:
   - **`ConnectionStrings:DefaultConnection`** – conexiune SQL Server (LocalDB sau instan??)
   - **`GoogleAI:ApiKey`** – cheie pentru Google Gemini API ([ob?ine?i aici](https://ai.google.dev/))

**Exemplu configurare:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=OnlineShop;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  "GoogleAI": {
    "ApiKey": "YOUR_GEMINI_API_KEY_HERE"
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

**Alternative:** Utiliza?i .NET Secret Manager pentru securitate sporit?:
```bash
cd OnlineShopProject_dNet
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "YOUR_CONNECTION_STRING"
dotnet user-secrets set "GoogleAI:ApiKey" "YOUR_GEMINI_KEY"
```

> ?? **Important:** NU commit-a?i `appsettings.json` în Git (deja în `.gitignore`)

---

## ?? Prezentare General?

Aplica?ie web de tip magazin online dezvoltat? în **ASP.NET Core MVC 9.0** cu **Entity Framework Core** ?i **ASP.NET Identity**. Implementeaz? sistem complet de e-commerce cu gestionare produse (CRUD, aprobat? colaboratori), categorii dinamice, co? de cump?r?turi, wishlist, review-uri cu rating ?i **asistent AI bazat pe Google Gemini** pentru întreb?ri despre produse.

---

## ??? Stack Tehnologic

<table>
<tr>
<td><strong>Backend</strong></td>
<td>ASP.NET Core MVC 9.0, C# 12</td>
</tr>
<tr>
<td><strong>Database</strong></td>
<td>Entity Framework Core 9.0, SQL Server</td>
</tr>
<tr>
<td><strong>Autentificare</strong></td>
<td>ASP.NET Identity (roluri: Admin, Proposer, User)</td>
</tr>
<tr>
<td><strong>Frontend</strong></td>
<td>Razor Views, Bootstrap 5</td>
</tr>
<tr>
<td><strong>AI Integration</strong></td>
<td>Google Gemini API (model: gemini-2.5-flash)</td>
</tr>
<tr>
<td><strong>Securitate</strong></td>
<td>Anti-Forgery Tokens, HtmlSanitizer, valid?ri server-side</td>
</tr>
<tr>
<td><strong>Logging</strong></td>
<td>Console/Debug, feedback TempData</td>
</tr>
</table>

---

## ? Func?ionalit??i Principale

### ?? Sistem de Roluri ?i Autentificare

| Rol | Permisiuni |
|-----|-----------|
| **Vizitator** | Vizualizare produse ?i review-uri; redirec?ionare la login pentru ac?iuni restric?ionate |
| **User** | Co?, comenzi, wishlist, ad?ugare/editare review-uri |
| **Proposer** | Propunere produse noi, editare/?tergere produse proprii (cu restric?ii status) |
| **Admin** | Control complet: aprobat?/respingere produse, CRUD categorii/produse/review-uri, gestionare utilizatori |

### ??? Gestionare Categorii Dinamice

Administratorii pot crea, edita ?i ?terge categorii din interfa??. Caracteristici:
- Nume unic ?i obligatoriu
- Cascade delete (?tergerea categoriei elimin? toate produsele asociate)
- Filtrare produse dup? categorie
- Afi?are în meniu de navigare

### ?? Gestionare Produse

**Câmpuri:** titlu, descriere, imagine, pre?, stoc, rating (1-5), review-uri

**Valid?ri implementate:**
- Pre? > 0, stoc ? 0
- Înc?rcare imagine: whitelist extensii (.jpg, .jpeg, .png, .gif), max 5MB
- Sanitizare HTML pentru descriere (protec?ie XSS)
- Rating calculat automat din media review-urilor

**Workflow colaboratori:**
- Produsele propuse intr? în status "Pending"
- Admin decide: "Approved" (vizibil public) sau "Rejected" (cu feedback)
- La editare, produsul revine în "Pending" pentru re-aprobat?
- Notific?ri c?tre autor cu decizia Admin

### ?? Co? de Cump?r?turi ?i Comenzi

- Co? persistent per utilizator (salvat în baza de date)
- Snapshot produs în `OrderDetail` (pre?, titlu, imagine) pentru istoric comenzi
- Validare stoc în timp real la fiecare opera?ie
- Decrement automat stoc la plasarea comenzii
- Istoric comenzi cu detalii complete

### ? Wishlist

- Lista personal? de produse favorite
- Cheie compus? `(UserId, ProductId)` previne duplicate
- Mutare rapid? din wishlist în co?
- Cascade delete la ?tergerea produsului

### ?? Review-uri ?i Rating

- Utilizatorii înregistra?i pot ad?uga/edita/?terge review-uri
- Text op?ional, rating op?ional (1-5)
- Recalcul automat al scorului produsului la orice modificare
- Restric?ie practic?: review doar pentru produse cump?rate

### ?? C?utare, Filtrare ?i Sortare

| Func?ionalitate | Detalii |
|----------------|---------|
| **C?utare** | Matching par?ial în titlu (ex: "lapto" ? "laptop") |
| **Filtrare** | Dup? categorie (ID sau nume) |
| **Sortare** | Pre?/Rating/Nume (cresc?tor/descresc?tor) |
| **Paginare** | Configurabil? (default: 12 produse/pagin?) |

### ?? Asistent AI pentru Produse (Google Gemini)

**Caracteristici:**
- Chat lateral pe fiecare pagin? de produs
- R?spunsuri generate din: descriere produs + FAQ + date categorii/stoc/pre?
- Prompt strict în român?: "R?spunde DOAR din informa?iile furnizate"
- Fallback controlat: *"Momentan nu avem detalii despre acest aspect."*
- Salvare întreb?ri frecvente în tabelul `FAQ`

**Securitate AI:**
- Validare input utilizator (max 500 caractere)
- Logging erori API
- Graceful degradation (dac? API-ul nu r?spunde, utilizatorul prime?te mesaj politicos)

---

## ??? Arhitectur? ?i Organizare Cod

<details>
<summary><strong>Controllers</strong></summary>

- `ProductsController` – CRUD produse, aprobat?/respingere, c?utare/filtrare
- `OrdersController` – co?, checkout, istoric comenzi
- `ReviewsController` – ad?ugare/editare/?tergere review-uri, recalcul rating
- `WishlistController` – gestionare wishlist
- `AdminController` – administrare utilizatori ?i platform?
- `ProductAIController` – integrare asistent AI
- `CategoriesController` – CRUD categorii
- `NotificationsController` – sistem notific?ri

</details>

<details>
<summary><strong>Services (Business Logic)</strong></summary>

- `GoogleProductAiService` – integrare Google Gemini API (IProductAiService)
- `ProductAIService` – fallback AI local + gestionare FAQ
- `CartService` – logic? co? de cump?r?turi
- `NotificationService` – notific?ri c?tre utilizatori
- `TextProcessingService` – procesare text (encoding, formatare)
- `HtmlSanitizationService` – sanitizare HTML (XSS protection)
- `ImageValidationService` – validare înc?rcare imagini

</details>

<details>
<summary><strong>Models (Entit??i Database)</strong></summary>

| Model | Descriere |
|-------|-----------|
| `ApplicationUser` | Utilizator (extends IdentityUser) |
| `Product` | Produs (titlu, descriere, pre?, stoc, status, rating) |
| `Category` | Categorie produse |
| `Review` | Review utilizator (text, rating, dat?) |
| `FAQ` | Întreb?ri frecvente (generale sau per produs) |
| `Order` | Comand? (user, dat?, adres? livrare, total, status) |
| `OrderDetail` | Linie comand? (snapshot produs, cantitate, pre? unitar) |
| `Wishlist` | Lista favorite (cheie compus? UserId+ProductId) |
| `Notification` | Notific?ri utilizatori (tip, mesaj, feedback, dat?) |

</details>

<details>
<summary><strong>Rela?ii Database (EF Core)</strong></summary>

```
Category 1??N Product (cascade delete)
Product 1??N Review (cascade delete)
Product 1??N OrderDetail (SetNull + snapshot)
Product 1??N Wishlist (cascade delete)
User 1??N Review (cascade delete)
User 1??N Order (cascade delete)
User 1??N Wishlist (cascade delete)
User 1??N Notification (cascade delete)
Order 1??N OrderDetail (cascade delete)
```

</details>

---

## ?? Instalare ?i Rulare

### Prerechizite

- [.NET SDK 9.0](https://dotnet.microsoft.com/download/dotnet/9.0)
- SQL Server (LocalDB sau instan?? accesibil?)
- [Google Gemini API Key](https://ai.google.dev/)

### Pa?i de Setup

**1?? Clonare repository:**
```bash
git clone https://github.com/EDward1101-bit/OnlineShopProject_dNet.git
cd OnlineShopProject_dNet
```

**2?? Configurare appsettings.json:**
```bash
cd OnlineShopProject_dNet
cp appsettings.example.json appsettings.json
# Edita?i appsettings.json ?i completa?i ConnectionStrings + GoogleAI:ApiKey
```

**3?? Restaurare pachete:**
```bash
dotnet restore
```

**4?? Instalare EF Core Tools (dac? nu exist?):**
```bash
dotnet tool update --global dotnet-ef
```

**5?? Aplicare migra?ii:**
```bash
cd OnlineShopProject_dNet
dotnet ef database update
```

**6?? Rulare aplica?ie:**
```bash
dotnet run
```

Aplica?ia va porni pe `https://localhost:[port]`. Deschide?i browser-ul la adresa afi?at?.

---

## ?? Utilizatori Demo (Seed Data)

La prima rulare, aplica?ia creeaz? automat 3 utilizatori de test:

| Email | Parol? | Rol |
|-------|--------|-----|
| `admin@test.com` | `Admin123!` | Admin |
| `proposer@test.com` | `Proposer123!` | Proposer (Colaborator) |
| `user@test.com` | `User123!` | User (Client) |

> ?? **Not?:** Aceste conturi sunt doar pentru dezvoltare. Modifica?i în produc?ie.

---

## ?? Securitate ?i Valid?ri

| M?sur? | Implementare |
|--------|--------------|
| **Anti-CSRF** | Filtru global `AutoValidateAntiforgeryToken` |
| **XSS Protection** | Sanitizare HTML cu `HtmlSanitizer` pentru descrieri/review-uri |
| **Upload Validation** | Whitelist extensii imagini + limit? 5MB |
| **Authorization** | Atribute `[Authorize(Roles="...")]` pe controllere |
| **Input Validation** | Data Annotations + valid?ri custom server-side |
| **Secrets Management** | `appsettings.json` în `.gitignore`, recomandare Secret Manager |

---

## ?? Integrare AI (Google Gemini)

### Configurare

Cheia API se configureaz? în `appsettings.json` sau prin Secret Manager:
```json
"GoogleAI": {
  "ApiKey": "YOUR_GEMINI_API_KEY"
}
```

### Workflow

1. Utilizatorul pune o întrebare pe pagina produsului
2. `GoogleProductAiService` construie?te un prompt structurat:
   - Date produs (titlu, descriere, pre?, stoc, categorie)
   - FAQ-uri relevante (generale + specifice produsului)
   - Întrebarea utilizatorului
   - Reguli stricte: "R?spunde DOAR din informa?iile furnizate, în român?, f?r? halucina?ii"
3. API Gemini (`gemini-2.5-flash`) genereaz? r?spuns
4. Parsing r?spuns JSON ?i returnare c?tre client
5. În caz de eroare sau informa?ii lips?: fallback fix *"Momentan nu avem detalii despre acest aspect."*

### Beneficii

- R?spunsuri instantanee la întreb?ri frecvente (garan?ie, compatibilitate, utilizare)
- Salvare întreb?ri utile în baza de date pentru îmbun?t??ire FAQ
- Reducere sarcin? customer support

---

## ?? Structur? Relevant? Fi?iere

```
OnlineShopProject_dNet/
??? Controllers/          # Logica MVC
?   ??? ProductsController.cs
?   ??? OrdersController.cs
?   ??? ReviewsController.cs
?   ??? WishlistController.cs
?   ??? AdminController.cs
?   ??? ProductAIController.cs
??? Models/               # Entit??i database
??? Views/                # Interfa?? Razor
??? Services/             # Business logic
?   ??? GoogleProductAiService.cs
?   ??? CartService.cs
?   ??? NotificationService.cs
?   ??? HtmlSanitizationService.cs
??? Data/
?   ??? ApplicationDbContext.cs
?   ??? SeedData.cs
?   ??? Migrations/
??? wwwroot/              # Static files (CSS, JS, images)
??? Program.cs            # Entry point + configurare servicii
??? appsettings.json      # Configurare (NU commit în Git!)
```

---

## ?? Observa?ii Importante

- Aplica?ia este configurat? pentru **SQL Server**. Pachetul SQLite este inclus dar nu este utilizat.
- F?r? `appsettings.json` valid sau f?r? chei API, aplica?ia **nu va porni**.
- Produsele "Pending" sunt vizibile doar autorului ?i Admin.
- Review-urile pot fi ad?ugate doar de utilizatori autentifica?i care au cump?rat produsul.
- Snapshot-urile din `OrderDetail` asigur? c? istoricul comenzilor r?mâne intact chiar dac? produsele sunt modificate/?terse.

---

## ?? Licen??

Acest proiect este pentru **uz educa?ional ?i demonstrativ**. Verifica?i politicile interne înainte de utilizare în produc?ie.

---

<div align="center">

**Dezvoltat cu ASP.NET Core MVC | Entity Framework Core | Google Gemini AI**

</div>