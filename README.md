# Aprovizionare

Aplicație web internă care leagă cele trei documente ale fluxului de aprovizionare: **comanda către furnizor, recepția în depozit și factura furnizorului**. Fiecare echipă lucrează cu contul ei, iar aplicația gestionează singură ce nu se potrivește: o factură pentru marfă nesosită sau cu alt preț decât cel comandat este blocată automat, iar comenzile întârziate și facturile blocate sunt anunțate zilnic pe email.

Backend: ASP.NET Core 10, EF Core, SQL Server. Frontend: React 19 cu Vite.


## Cum rulez aplicația

### Ce trebuie instalat

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) 22 sau mai nou (testat cu 24)
- SQL Server: Express, Developer sau LocalDB

### 1. Baza de date

Connection string-ul e în `Aprovizionare.Api/appsettings.json`:

```json
"ConnectionStrings": {
  "Default": "Server=localhost\\SQLEXPRESS;Database=Aprovizionare;Trusted_Connection=True;TrustServerCertificate=True"
}
```

Dacă folosiți altă instanță, modificați `Server`. Pentru LocalDB: `Server=(localdb)\\MSSQLLocalDB`.

**Nu e nevoie de niciun script SQL.** La pornire, aplicația aplică singură migrările, creează baza de date și adaugă datele de test: utilizatorii, 3 furnizori și 5 produse.

### 2. API-ul

```powershell
cd Aprovizionare.Api
dotnet run --launch-profile http
```

API-ul pornește pe `http://localhost:5269`. Din Visual Studio merge cu oricare profil (`http` sau `https`).

### 3. Frontend-ul

Într-un al doilea terminal:

```powershell
cd frontend
npm install
npm run dev
```

Aplicația se deschide la **http://localhost:5173**. Vite trimite cererile `/api` către API, deci nu e nevoie de configurare CORS.

### 4. Conturile de test

Toate au parola `Parola123!`. Pe ecranul de login, un click pe un cont îl completează automat.

| Email | Rol | Ce poate face |
| --- | --- | --- |
| `andrei@firma.ro` | Achiziții | creează și anulează comenzi; vede recepțiile și facturile |
| `maria@firma.ro` | Depozit | înregistrează recepții; vede comenzile **fără prețuri** |
| `elena@firma.ro` | Contabilitate | înregistrează, aprobă, respinge și plătește facturi |
| `admin@firma.ro` | Administrator | tot, plus alertele pe email și jurnalul de audit |

### 5. Testele

```powershell
dotnet test
```

23 de teste unitare, pentru regulile de business: împărțirea recepției pe comandă, three-way match, cursul BNR și alertele.

### 6. Emailurile

Implicit, emailurile **nu sunt trimise**, ci salvate ca fișiere `.eml` în `Aprovizionare.Api/emails`. Se deschid cu dublu-click în Outlook sau în aplicația Mail.

Pentru trimitere reală, completați `Email:SmtpHost` (și, la nevoie, `SmtpPort`, `SmtpUser`, `SmtpPassword`, `SmtpUseSsl`) în `appsettings.Development.json`. Pentru un test local se poate folosi [Mailpit](https://github.com/axllent/mailpit): porniți `mailpit.exe` dintr-un terminal, setați `"SmtpHost": "localhost"` și `"SmtpPort": 1025`, apoi deschideți `http://localhost:8025`.

Alertele pleacă automat zilnic la ora din `Alerts:DailyAt` (implicit 08:00). Ca Admin, pagina „Alerte pe email” arată ce ar pleca acum și le poate trimite imediat.

### Structura proiectului

| Folder | Conținut |
| --- | --- |
| `Aprovizionare.Api/Entities`, `Enums` | modelul de domeniu |
| `Aprovizionare.Api/Data` | `AppDbContext`, interceptorul de audit, datele inițiale |
| `Aprovizionare.Api/Dtos` | ce intră și ce iese din API |
| `Aprovizionare.Api/Services` | serviciile, regulile pure, cursul BNR, emailurile, jobul zilnic |
| `Aprovizionare.Api/Controllers` | endpoint-urile |
| `Aprovizionare.Api/Migrations` | migrările EF Core |
| `Aprovizionare.Tests` | testele unitare (xUnit) |
| `frontend/src` | aplicația React: `pages`, `components`, `api.js`, `roles.js` |

### Endpoint-urile principale

| Metodă și adresă | Ce face |
| --- | --- |
| `POST /api/auth/login`, `POST /api/auth/logout`, `GET /api/auth/me` | autentificare |
| `GET`, `POST /api/purchase-orders`, `POST /api/purchase-orders/{id}/cancel` | comenzi |
| `GET`, `POST /api/goods-receipts` | recepții |
| `GET /api/stock` | stocul |
| `GET`, `POST /api/invoices`, `POST /api/invoices/{id}/recheck`, `approve`, `pay`, `reject` | facturi |
| `GET /api/suppliers`, `GET /api/products` | catalogul |
| `GET /api/audit` | jurnalul de audit (Admin) |
| `GET /api/alerts/preview`, `POST /api/alerts/send-now` | alertele pe email (Admin) |

Erorile de business răspund cu 400 și `{ "message": "..." }`, în română.

## Decizii și presupuneri

**Despre flux**

- **Fiecare persoană are propriul cont, cu rolul echipei.** Enunțul spune „fiecare echipă cu contul ei”. Cu un cont comun pe echipă, auditul n-ar putea spune *cine* a făcut o acțiune.
- **Depozitul introduce doar ce a numărat.** Aplicația împarte singură cantitatea pe liniile deschise ale comenzii. Ce depășește cantitatea comandată și produsele care nu sunt pe comandă devin linii „de clarificat”. Prin urmare, pe o linie de comandă nu se recepționează niciodată mai mult decât s-a comandat.
- **Recepția poate exista fără comandă**, pentru marfa venită fără să fi fost comandată. Stocul include toată marfa intrată fizic, inclusiv surplusul.
- **O comandă cu recepții nu mai poate fi anulată**, iar o comandă anulată nu mai poate primi recepții sau facturi.

**Despre facturi**

- **Three-way match:** pentru fiecare linie de comandă, cantitatea facturată în total (inclusiv pe facturile anterioare nerespinse) trebuie să fie ≤ cantitatea recepționată, iar prețul trebuie să fie egal cu cel din comandă, cu o toleranță de 0,01 pentru rotunjiri.
- **O factură blocată poate fi aprobată doar cu o justificare scrisă**, salvată pe factură și în audit. Butonul „Reverifică” reia verificarea, de exemplu când factura a sosit înaintea mărfii.
- **Factura trebuie să aibă aceeași monedă ca comanda**, altfel prețurile nu pot fi comparate.
- **Numărul facturii e unic pe furnizor**, nu global: fiecare furnizor își numerotează facturile separat.

**Despre cursul valutar**

- Se folosește cursul de referință BNR **din data facturii**, cum cere enunțul. În weekend și de sărbători, ultimul curs publicat înainte. Legislația fiscală folosește pentru TVA cursul comunicat în ziua anterioară facturii; schimbarea ar fi într-o singură metodă (`FindRateForDate`).
- **Cursul se salvează pe factură**, ca valoarea în RON a unei facturi aprobate să nu se schimbe ulterior. Tabelul de cursuri e doar un cache.
- **Adresa BNR e configurabilă** (`Bnr:BaseUrl`). Adresa clasică `www.bnr.ro/nbrfxrates.xml` întoarce acum pagina principală a site-ului, cu cod 200, iar fișierele XML sunt pe `curs.bnr.ro`. Erorile de rețea, timeout-ul și un XML invalid devin un mesaj clar pentru utilizator, nu o eroare 500.

**Tehnice**

- **Ștergerile în cascadă sunt dezactivate** (`Restrict`), ca o comandă cu facturi să nu poată dispărea din greșeală. Enum-urile sunt salvate ca text, ca baza de date să fie lizibilă.
- **Tabelele Identity nu sunt auditate**, pentru că rândul utilizatorului conține hash-ul parolei.
- **Alertele sunt calculate din starea curentă**, nu salvate: o problemă apare în emailul zilnic până când e rezolvată.
- **Cookie în loc de JWT**, pentru că aplicația e internă, iar frontend-ul și API-ul au aceeași adresă prin proxy. Cookie-ul e `HttpOnly`, deci nu poate fi citit din JavaScript.