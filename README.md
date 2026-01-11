<div align="center">GearUp - Online Gym Gear Shop</div>

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

## ⚠️ Configurație Necesară (Înainte de Rulare)

> **Proiectul nu va porni fără configurarea corectă a fișierului `appsettings.json`**

**Pașii obligatorii:**

1. Creați fișierul `OnlineShopProject_dNet/appsettings.json` sau redenumiți `appsettings.example.json`
2. Completați valorile necesare:
   - **`ConnectionStrings:DefaultConnection`** – conexiune SQL Server (LocalDB sau instanță)
   - **`GoogleAI:ApiKey`** – cheie pentru Google Gemini API ([obțineți aici](https://ai.google.dev/))

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

**Alternative:** Utilizați .NET Secret Manager pentru securitate sporită:
```bash
cd OnlineShopProject_dNet
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "YOUR_CONNECTION_STRING"
dotnet user-secrets set "GoogleAI:ApiKey" "YOUR_GEMINI_KEY"
```

> 🔒 **Important:** NU commit-ați `appsettings.json` în Git (deja în `.gitignore`)

---

## 📋 Prezentare Generală

Aplicație web de tip magazin online dezvoltată în **ASP.NET Core MVC 9.0** cu **Entity Framework Core** și **ASP.NET Identity**. Implementează sistem complet de e-commerce cu gestionare produse (CRUD, aprobată colaboratori), categorii dinamice, coș de cumpărături, wishlist, review-uri cu rating și **asistent AI bazat pe Google Gemini** pentru întrebări despre produse.

---

## 🛠️ Stack Tehnologic

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
<td>Anti-Forgery Tokens, HtmlSanitizer, validări server-side</td>
</tr>
<tr>
<td><strong>Logging</strong></td>
<td>Console/Debug, feedback TempData</td>
</tr>
</table>

---

## ✨ Funcționalități Principale

### 🔐 Sistem de Roluri și Autentificare

| Rol | Permisiuni |
|-----|-----------|
| **Vizitator** | Vizualizare produse și review-uri; redirecționare la login pentru acțiuni restricționate |
| **User** | Coș, comenzi, wishlist, adăugare/editare review-uri |
| **Proposer** | Propunere produse noi, editare/ștergere produse proprii (cu restricții status) |
| **Admin** | Control complet: aprobată/respingere produse, CRUD categorii/produse/review-uri, gestionare utilizatori |

### 🗂️ Gestionare Categorii Dinamice

Administratorii pot crea, edita și șterge categorii din interfață. Caracteristici:
- Nume unic și obligatoriu
- Cascade delete (ștergerea categoriei elimină toate produsele asociate)
- Filtrare produse după categorie
- Afișare în meniu de navigare

### 📦 Gestionare Produse

**Câmpuri:** titlu, descriere, imagine, preț, stoc, rating (1-5), review-uri

**Validări implementate:**
- Preț > 0, stoc ≥ 0
- Încărcare imagine: whitelist extensii (.jpg, .jpeg, .png, .gif), max 5MB
- Sanitizare HTML pentru descriere (protecție XSS)
- Rating calculat automat din media review-urilor

**Workflow colaboratori:**
- Produsele propuse intră în status "Pending"
- Admin decide: "Approved" (vizibil public) sau "Rejected" (cu feedback)
- La editare, produsul revine în "Pending" pentru re-aprobată
- Notificări către autor cu decizia Admin

### 🛒 Coș de Cumpărături și Comenzi

- Coș persistent per utilizator (salvat în baza de date)
- Snapshot produs în `OrderDetail` (preț, titlu, imagine) pentru istoric comenzi
- Validare stoc în timp real la fiecare operație
- Decrement automat stoc la plasarea comenzii
- Istoric comenzi cu detalii complete

### ⭐ Wishlist

- Lista personală de produse favorite
- Cheie compusă `(UserId, ProductId)` previne duplicate
- Mutare rapidă din wishlist în coș
- Cascade delete la ștergerea produsului

### 💬 Review-uri și Rating

- Utilizatorii înregistrați pot adăuga/edita/șterge review-uri
- Text opțional, rating opțional (1-5)
- Recalcul automat al scorului produsului la orice modificare
- Restricție practică: review doar pentru produse cumpărate

### 🔍 Căutare, Filtrare și Sortare

| Funcționalitate | Detalii |
|----------------|---------|
| **Căutare** | Matching parțial în titlu (ex: "lapto" → "laptop") |
| **Filtrare** | După categorie (ID sau nume) |
| **Sortare** | Preț/Rating/Nume (crescător/descrescător) |
| **Paginare** | Configurabilă (default: 12 produse/pagină) |

### 🤖 Asistent AI pentru Produse (Google Gemini)

**Caracteristici:**
- Chat lateral pe fiecare pagină de produs
- Răspunsuri generate din: descriere produs + FAQ + date categorii/stoc/preț
- Prompt strict în română: "Răspunde DOAR din informațiile furnizate"
- Fallback controlat: *"Momentan nu avem detalii despre acest aspect."*
- Salvare întrebări frecvente în tabelul `FAQ`

**Securitate AI:**
- Validare input utilizator (max 500 caractere)
- Logging erori API
- Graceful degradation (dacă API-ul nu răspunde, utilizatorul primește mesaj politicos)

---

## 🏗️ Arhitectură și Organizare Cod

<details>
<summary><strong>Controllers</strong></summary>

- `ProductsController` – CRUD produse, aprobată/respingere, căutare/filtrare
- `OrdersController` – coș, checkout, istoric comenzi
- `ReviewsController` – adăugare/editare/ștergere review-uri, recalcul rating
- `WishlistController` – gestionare wishlist
- `AdminController` – administrare utilizatori și platformă
- `ProductAIController` – integrare asistent AI
- `CategoriesController` – CRUD categorii
- `NotificationsController` – sistem notificări

</details>

<details>
<summary><strong>Services (Business Logic)</strong></summary>

- `GoogleProductAiService` – integrare Google Gemini API (IProductAiService)
- `ProductAIService` – fallback AI local + gestionare FAQ
- `CartService` – logică coș de cumpărături
- `NotificationService` – notificări către utilizatori
- `TextProcessingService` – procesare text (encoding, formatare)
- `HtmlSanitizationService` – sanitizare HTML (XSS protection)
- `ImageValidationService` – validare încărcare imagini

</details>

<details>
<summary><strong>Models (Entități Database)</strong></summary>

| Model | Descriere |
|-------|-----------|
| `ApplicationUser` | Utilizator (extends IdentityUser) |
| `Product` | Produs (titlu, descriere, preț, stoc, status, rating) |
| `Category` | Categorie produse |
| `Review` | Review utilizator (text, rating, dată) |
| `FAQ` | Întrebări frecvente (generale sau per produs) |
| `Order` | Comandă (user, dată, adresă livrare, total, status) |
| `OrderDetail` | Linie comandă (snapshot produs, cantitate, preț unitar) |
| `Wishlist` | Lista favorite (cheie compusă UserId+ProductId) |
| `Notification` | Notificări utilizatori (tip, mesaj, feedback, dată) |

</details>

<details>
<summary><strong>Relații Database (EF Core)</strong></summary>

```
Category 1──N Product (cascade delete)
Product 1──N Review (cascade delete)
Product 1──N OrderDetail (SetNull + snapshot)
Product 1──N Wishlist (cascade delete)
User 1──N Review (cascade delete)
User 1──N Order (cascade delete)
User 1──N Wishlist (cascade delete)
User 1──N Notification (cascade delete)
Order 1──N OrderDetail (cascade delete)
```

</details>

---

## 🚀 Instalare și Rulare

### Prerechizite

- [.NET SDK 9.0](https://dotnet.microsoft.com/download/dotnet/9.0)
- SQL Server (LocalDB sau instanță accesibilă)
- [Google Gemini API Key](https://ai.google.dev/)

### Pași de Setup

**1️⃣ Clonare repository:**
```bash
git clone https://github.com/EDward1101-bit/OnlineShopProject_dNet.git
cd OnlineShopProject_dNet
```

**2️⃣ Configurare appsettings.json:**
```bash
cd OnlineShopProject_dNet
cp appsettings.example.json appsettings.json
# Editați appsettings.json și completați ConnectionStrings + GoogleAI:ApiKey
```

**3️⃣ Restaurare pachete:**
```bash
dotnet restore
```

**4️⃣ Instalare EF Core Tools (dacă nu există):**
```bash
dotnet tool update --global dotnet-ef
```

**5️⃣ Aplicare migrații:**
```bash
cd OnlineShopProject_dNet
dotnet ef database update
```

**6️⃣ Rulare aplicație:**
```bash
dotnet run
```

Aplicația va porni pe `https://localhost:[port]`. Deschideți browser-ul la adresa afișată.

---

## 👥 Utilizatori Demo (Seed Data)

La prima rulare, aplicația creează automat 3 utilizatori de test:

| Email | Parolă | Rol |
|-------|--------|-----|
| `admin@test.com` | `Admin123!` | Admin |
| `proposer@test.com` | `Proposer123!` | Proposer (Colaborator) |
| `user@test.com` | `User123!` | User (Client) |

> ⚠️ **Notă:** Aceste conturi sunt doar pentru dezvoltare. Modificați în producție.

---

## 🔒 Securitate și Validări

| Măsură | Implementare |
|--------|--------------|
| **Anti-CSRF** | Filtru global `AutoValidateAntiforgeryToken` |
| **XSS Protection** | Sanitizare HTML cu `HtmlSanitizer` pentru descrieri/review-uri |
| **Upload Validation** | Whitelist extensii imagini + limită 5MB |
| **Authorization** | Atribute `[Authorize(Roles="...")]` pe controllere |
| **Input Validation** | Data Annotations + validări custom server-side |
| **Secrets Management** | `appsettings.json` în `.gitignore`, recomandare Secret Manager |

---

## 📊 Integrare AI (Google Gemini)

### Configurare

Cheia API se configurează în `appsettings.json` sau prin Secret Manager:
```json
"GoogleAI": {
  "ApiKey": "YOUR_GEMINI_API_KEY"
}
```

### Workflow

1. Utilizatorul pune o întrebare pe pagina produsului
2. `GoogleProductAiService` construiește un prompt structurat:
   - Date produs (titlu, descriere, preț, stoc, categorie)
   - FAQ-uri relevante (generale + specifice produsului)
   - Întrebarea utilizatorului
   - Reguli stricte: "Răspunde DOAR din informațiile furnizate, în română, fără halucinații"
3. API Gemini (`gemini-2.5-flash`) generează răspuns
4. Parsing răspuns JSON și returnare către client
5. În caz de eroare sau informații lipsă: fallback fix *"Momentan nu avem detalii despre acest aspect."*

### Beneficii

- Răspunsuri instantanee la întrebări frecvente (garanție, compatibilitate, utilizare)
- Salvare întrebări utile în baza de date pentru îmbunătățire FAQ
- Reducere sarcină customer support

---

## 📂 Structură Relevantă Fișiere

```
OnlineShopProject_dNet/
├── Controllers/          # Logica MVC
│   ├── ProductsController.cs
│   ├── OrdersController.cs
│   ├── ReviewsController.cs
│   ├── WishlistController.cs
│   ├── AdminController.cs
│   └── ProductAIController.cs
├── Models/               # Entități database
├── Views/                # Interfață Razor
├── Services/             # Business logic
│   ├── GoogleProductAiService.cs
│   ├── CartService.cs
│   ├── NotificationService.cs
│   └── HtmlSanitizationService.cs
├── Data/
│   ├── ApplicationDbContext.cs
│   ├── SeedData.cs
│   └── Migrations/
├── wwwroot/              # Static files (CSS, JS, images)
├── Program.cs            # Entry point + configurare servicii
└── appsettings.json      # Configurare (NU commit în Git!)
```

---

## 📝 Observații Importante

- Aplicația este configurată pentru **SQL Server**. Pachetul SQLite este inclus dar nu este utilizat.
- Fără `appsettings.json` valid sau fără chei API, aplicația **nu va porni**.
- Produsele "Pending" sunt vizibile doar autorului și Admin.
- Review-urile pot fi adăugate doar de utilizatori autentificați care au cumpărat produsul.
- Snapshot-urile din `OrderDetail` asigură că istoricul comenzilor rămâne intact chiar dacă produsele sunt modificate/șterse.

---

## 📄 Licență

Acest proiect este pentru **uz educațional și demonstrativ**. Verificați politicile interne înainte de utilizare în producție.

---

<div align="center">

**Dezvoltat cu ASP.NET Core MVC | Entity Framework Core | Google Gemini AI**

</div>
