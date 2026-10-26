# PROJECT_CONTEXT — Appointment Booking API i Mobile

> **Datum analize:** 2026-10-10  
> **API projekat:** `C:\Users\Korisnik\Desktop\Appointment Booking\AppointmentBooking.Api`  
> **Mobile projekat:** `C:\Users\Korisnik\Desktop\Appointment Booking\AppointmentBooking.Mobile`  
> **Tehnologije:** ASP.NET Core Web API / .NET 10, PostgreSQL 18, Dapper, Npgsql, Redis; .NET MAUI / .NET 10, C#, XAML, CommunityToolkit.Mvvm i CommunityToolkit.Maui.  
> **Sažetak:** API upravlja korisnicima, zaposlenicima, uslugama, raspoloživošću i rezervacijama. Mobile aplikacija koristi isti API za korisničke rezervacije i staff/admin pregled i upravljanje terminima. Ovaj dokument opisuje stanje koje je potvrđeno pregledom izvornog koda na navedeni datum; ne predstavlja specifikaciju budućeg ponašanja.

## 1. Opseg i stanje radnog stabla

Analizirani su API i Mobile izvorni projekti u navedenim folderima. U zajedničkom folderu postoji i `appointment-booking-frontend`, ali nije dio ovog pregleda jer je zadatak ograničen na API i Mobile.

Oba Git repozitorija su na granama `implement-admin-staff` i `implement-staff-panel`. U oba su pronađene necommitovane promjene. Ovaj dokument zato opisuje trenutno prisutne fajlove, uključujući izmjene iz radnog stabla, a ne samo posljednji commit. Među necommitovanim dijelovima su kalendar radnih dana, prikaz telefona salona, staff booking tok i staff panel. Prije budućeg rada treba provjeriti `git status` i aktuelni sadržaj fajlova.

Pored glavnih projekata, postoji pomoćna Android kopija na `C:\src\AppointmentBooking.Mobile`. Ona nije korištena kao kanonski Mobile korijen. Njena konfiguracija u `ProfileViewModel` ima drugi hardkodirani `SalonId` (`4` naspram `1` u glavnom Mobile projektu), pa se te kopije ne smiju smatrati identičnim bez dodatne provjere.

## 2. Sistemska arhitektura

```text
Mobile (.NET MAUI)
  Views (XAML) → ViewModels → IApiClient / IAuthApiClient
                         ↓ HTTPS + JSON + Bearer JWT
API (ASP.NET Core)
  Controllers → Services → Repositories (Dapper + raw SQL)
                         ↓
                 PostgreSQL / Redis
```

ASP.NET Core registruje kontrolere, JWT autentifikaciju, autorizaciju, globalni exception handler, OpenAPI, CORS, `NpgsqlDataSource`, Redis multiplexer i servisno/repozitorijske implementacije u `AppointmentBooking.Api/Program.cs`. Mobile registruje stranice i ViewModel-e, `HttpClient` klijente, sesiju i skladištenje refresh tokena u `AppointmentBooking.Mobile/MauiProgram.cs`.

Mobile serializira/deserializira JSON preko `System.Text.Json`. Glavni API klijent koristi camelCase property naming i `JsonStringEnumConverter`; API takođe vraća enum vrijednosti kao JSON stringove. Zaštićeni Mobile pozivi prolaze kroz `BearerTokenHandler`, koji dodaje `Authorization: Bearer ...`, a na `401` pokušava jednom obnoviti sesiju i ponoviti zahtjev.

## 3. API projekat

### 3.1. Tehnologije i ulazna tačka

- `AppointmentBooking.Api/AppointmentBooking.Api.csproj`: `net10.0`, nullable i implicit usings uključeni.
- Biblioteke: Dapper `2.1.79`, Npgsql `10.0.3`, `Microsoft.AspNetCore.Authentication.JwtBearer` `10.0.11`, `Microsoft.AspNetCore.OpenApi` `10.0.9`, `Swashbuckle.AspNetCore.SwaggerUI` `10.2.3`, `StackExchange.Redis` `3.1.31`, `System.IdentityModel.Tokens.Jwt` `8.22.0`.
- Nema Entity Framework Core; pristup bazi koristi `NpgsqlDataSource`, `NpgsqlConnection`, Dapper i SQL stringove u `Repositories`.
- Ulazna tačka: `AppointmentBooking.Api/Program.cs`.
- Lokalni profili su u `AppointmentBooking.Api/Properties/launchSettings.json`: HTTP `http://localhost:5208`, HTTPS `https://localhost:7236`; Development profil otvara Swagger.
- `AppointmentBooking.Api/Database` sadrži numerisane SQL skripte 001–014. One nisu EF migracije i aplikacija ih ne primjenjuje automatski.
- Docker: `AppointmentBooking.Api/Dockerfile` koristi .NET 10 SDK multi-stage build. `compose.yaml` definiše PostgreSQL 18, API i Redis.

### 3.2. Folderi i slojevi

| Folder | Odgovornost |
|---|---|
| `Controllers/` | HTTP rute, atributi validacije/autorizacije, poziv servisa i HTTP statusi. |
| `Services/` | Poslovna pravila, orkestracija, provjera identiteta i mapiranje modela u response DTO. |
| `Repositories/` | Dapper/raw SQL upiti, transakcije, PostgreSQL čitanje i upis te Redis hold implementacija. |
| `DTOs/` | Odvojeni request/response ugovori grupisani po funkcionalnosti. |
| `Models/` | Interni modeli, enum-i i strukture podataka. |
| `Database/` | Početna shema, izmjene sheme i seed podaci. |
| `BackgroundServices/` | Periodično automatsko dovršavanje; cleanup servis trenutno ima komentarisanu implementaciju. |
| `Configuration/` | Tipizirane opcije `BookingOptions`, `JwtOptions`, `MailjetOptions`, `RedisOptions`. |
| `Exceptions/` | Domenske iznimke i `GlobalExceptionHandler` koji vraća `ProblemDetails`. |
| `OpenApi/` | Schema transformer za `EmployeeWorkingHours`. |
| `AppointmentBooking.Api.Tests/` | xUnit testovi servisnog sloja, validacije i HTTP integracije. |

### 3.3. Konfiguracija, sigurnost i greške

`Program.cs` očekuje konfiguraciju:

| Sekcija/ključ | Namjena |
|---|---|
| `ConnectionStrings:Database` | PostgreSQL veza, koristi se pri kreiranju `NpgsqlDataSource`. |
| `Jwt:SecretKey`, `Jwt:Issuer`, `Jwt:Audience`, `Jwt:AccessTokenMinutes`, `Jwt:RefreshTokenDays` | JWT potpis, validacija i rokovi tokena. Tajne vrijednosti se ne uključuju u dokumentaciju. |
| `Redis:ConnectionString` | Redis multiplexer za privremene holdove. |
| `Bookings:HoldDurationInMinutes`, `AvailabilitySlotIntervalMinutes` | Hold rok i granularnost availability slotova. |
| `Mailjet:ApiKey`, `SecretKey`, `FromEmail`, `FromName` | Mailjet konfiguracija u `MailjetOptions`; vrijednosti nisu kopirane. |

Konfiguraciju treba dostaviti preko odgovarajuće konfiguracije .NET-a, environment varijabli, user secrets ili Docker okruženja. Primjeri naziva environment ključeva u Compose-u su `ConnectionStrings__Database` i `Jwt__SecretKey`. Nijedna tajna vrijednost nije prenesena u ovaj dokument.

JWT validacija provjerava issuer, audience, signing key i lifetime, uz `ClockSkew = 0`. `ClaimTypes.Role` se koristi kao role claim. `AddAuthorization()` nema globalni fallback policy: endpoint je zaštićen samo ako kontroler ili akcija imaju `[Authorize]`. Neke upravljačke rute nemaju taj atribut; detalji su navedeni u tabeli ruta i to je potencijalno važno sigurnosno ograničenje.

`GlobalExceptionHandler` mapira greške u RFC-style `ProblemDetails`: validacija 400, not-found 404, konflikt/slot zauzet 409, neautorizovan domen 401, konekcijska nedostupnost baze 503, ostalo 500. Dodaje `traceId`; ne izlaže detalje neočekivane greške klijentu. `AddProblemDetails()` je registrovan. CORS policy `Frontend` dopušta `http://localhost:5173` i HTTPS ekvivalent.

U Development okruženju `MapOpenApi()` izlaže `/openapi/v1.json`, a Swagger UI je na `/swagger`.

### 3.4. PostgreSQL shema

Izvor sheme je skup SQL skripti. Konačna shema se formira primjenom skripti po redoslijedu; kolone dodane kasnijim skriptama nisu nužno vidljive u početnom `001_schema.sql`.

| Tabela | Važne kolone i ograničenja |
|---|---|
| `user_accounts` | `id BIGINT IDENTITY`, ime/prezime, `email CITEXT UNIQUE`, `password_hash`, `role`, `is_active`, vremenske oznake. `role` je ograničen na `customer`, `employee`, `admin`. |
| `customers` | `id`, `user_account_id` unique FK na `user_accounts`, `phone VARCHAR(30)`, vremenske oznake. |
| `employees` | `id`, `user_account_id` unique FK na `user_accounts`, `phone`, `is_active`, vremenske oznake. |
| `services` | `id`, `name` unique, `description`, `duration_minutes`, `price NUMERIC(10,2)`, `is_active`, vremenske oznake. Trajanje mora biti pozitivno, cijena nenegativna. |
| `employee_services` | Složeni PK (`employee_id`, `service_id`) i FK-ovi prema employee/service. Migracija 002 dodaje `is_active`, `created_at`, `updated_at`; postoji indeks po `service_id`. |
| `employee_working_hours` | `employee_id`, `day_of_week SMALLINT`, `starts_at TIME`, `ends_at TIME`, generisani `time_range INT4RANGE`; FK na employee. Dan 1–7, kraj mora biti kasnije od početka. GIST exclusion constraint sprečava preklapanje sati za istog zaposlenika i dan; interval je `[)`, pa susjedni rasponi mogu dodirivati. |
| `bookings` | `customer_id` FK (u migraciji 013 postaje nullable), `employee_id` FK, `starts_at/ends_at TIMESTAMPTZ`, `status`, `notes`, vremenske oznake. Ograničava pozitivan raspon i status `scheduled/completed/cancelled`; GIST exclusion sprečava preklapanje scheduled termina istog employee-a. Migracija 009 dodaje `completed_at`, `completion_source` (`manual`/`automatic`) i indeks scheduled termina po `ends_at`. Migracija 013 dodaje `customer_first_name`/`customer_last_name` za walk-in rezervacije i XOR-like check: ili registrovani `customer_id`, ili oba imena gosta. |
| `appointments` | Stavke usluga vezane preko `booking_id`, `service_id`, vrijeme, status i vremenske oznake. Migracija 008 dodaje snapshot `service_name_at_booking`, `duration_minutes_at_booking`, `price_at_booking` sa ograničenjima trajanja/cijene, da kasnije izmjene usluge ne promijene historijske podatke. |
| `user_refresh_tokens` | FK na user account, unique `token_hash`, `expires_at`, `revoked_at`, `replaced_by_token_id` self-FK. Migracija 007 dodaje family ID/index za rotaciju refresh tokena. |
| `password_reset_codes` | FK na korisnika, `code_hash`, expiry, `attempts`, `verified_at`, `used_at`, created timestamp; indeksira korisnika i vrijeme. |
| `employee_unavailability` | FK `employee_id`, `block_type SMALLINT`, `starts_at/ends_at TIMESTAMPTZ`, `is_all_day`, `note`, `created_at`. Tip 1=Break, 2=Vacation; kraj poslije početka; Break nije all-day, Vacation jeste. Indeks po employee/range. |
| `salons` | `id`, naziv, adresa, grad, `instagram_url`, `facebook_url`, koordinate, `is_active`, timestamps. Migracija 014 dodaje `phone VARCHAR(30)`. Koordinate su ograničene na geografske vrijednosti i moraju biti oba NULL ili oba popunjena. |
| `salon_working_hours` | FK na salon (cascade delete), dan 1–7, `starts_at/ends_at TIME`, generisani range i exclusion constraint za preklapanja po salonu/danu. |

`booking_holds` nije PostgreSQL tabela u pregledanim skriptama: privremeni hold podaci se čuvaju u Redis-u preko `BookingHoldRepository`. Konačna shema za ovu tabelu/vezu je stoga neprimjenjiva.

**Relacije:** korisnički račun je 1:1 sa customer ili employee zapisom; employee–service je M:N preko `employee_services`; booking povezuje employee-a i opcionalnog registrovanog customer-a; `appointments` čuva jednu stavku po odabranoj usluzi; salon ima više radnih intervala.

**Datumi i novac:** termini i izostanci koriste `DateTimeOffset` u C# i `TIMESTAMPTZ` u PostgreSQL-u. Radni sati su `TIME`/`TimeOnly` bez zone. Cijene su `decimal` / `NUMERIC(10,2)`. Serijalizacija enum-a u API-ju je string, dok se DB statusi čuvaju malim slovima. Availabilty izračun koristi `TimeZoneInfo.Local` i `DateTimeOffset.Now`; eksplicitna poslovna vremenska zona za salon nije pronađena. Ovo je važno kod API-ja pokrenutog u Dockeru, gdje sistemska zona može biti drugačija od zone uređaja.

**Seed:** Demo seed skripta je uklonjena. Compose inicijalizuje samo shemu, bez početnih salona, korisnika, usluga ili termina.

**Compose/migracije:** `compose.yaml` mounta SQL shema skripte 001–005 i 007–014 u `docker-entrypoint-initdb.d`; demo seed se ne pokreće. PostgreSQL init skripte se izvršavaju samo pri prvom inicijalizovanju praznog volumena; dodavanje ili izmjena skripti ne mijenja već postojeći volumen.

### 3.5. Booking poslovna logika

- Customer rezervacija koristi dvofazni tok: `POST /api/bookings/hold` validira zaposlenika, aktivne usluge i assignment-e, izračunava trajanje/raspored, provjerava radne sate, izostanke, zakazane termine i aktivne holdove. Hold ima GUID token i vrijeme isteka te se čuva u Redis-u. Nakon toga `POST /api/bookings/{holdToken}/confirm` kreira booking i appointment stavke u PostgreSQL transakciji.
- Svaka appointment stavka čuva snapshot naziva, trajanja i cijene usluge. Ukupno trajanje i cijena se računaju sabiranjem tih snapshot vrijednosti.
- Za staff/admin ručni booking `POST /api/bookings/staff` prima ime/prezime osobe bez računa. Zaposlenik može kreirati rezervaciju samo za sebe; admin mora poslati `EmployeeId`. Servis validira vrijeme u budućnosti, usluge, radno vrijeme, izostanke, scheduled overlap i Redis hold. Gost se čuva u `bookings.customer_first_name/last_name` sa `customer_id = NULL`, ne u `customers`.
- Availability je po zaposleniku, datumu i listi service ID-jeva. Trajanje usluga određuje trajanje svakog predloženog termina. Slotovi se pomjeraju po `AvailabilitySlotIntervalMinutes` i kandidat mora stati u radni interval. Dan u prošlosti i vikendi vraćaju prazne slotove. Preklapanja se porede kao half-open intervali: `candidateStart < block.EndsAt && candidateEnd > block.StartsAt`; zbog toga slot koji počinje tačno kada pauza završava nije preklop.
- Availability odbacuje termine koji se preklapaju sa employee unavailability, scheduled bookingom ili aktivnim Redis holdom. Vacation all-day datumi takođe uklanjaju datum iz listi radnih dana.
- `GET /api/bookings/calendar-days` vraća datume, ne slotove. Trenutna izmjena traži prethodnih 30 i budućih do 60 radnih datuma, filtrira vikende, oslanja se na employee working-hours dane i izbacuje all-day vacation. `WorkingDates` je sortirana kolekcija. U sadašnjoj implementaciji buduća kolekcija uzima datume `>= today`, pa ako je danas radni dan, danas ulazi među tih 60.
- Cancel/complete status operacije imaju role i ownership provjere u servisu. Customer vidi svoje rezervacije; employee vidi svoje booking-e; admin može dobiti admin listu. Completed ne može prije završetka termina; cancelled ne može biti dovršen. Dovršavanje termina se obavlja ručno u staff toku.
- API nema registrovane background service-e. Hold zapis u Redis-u ističe preko TTL-a; potvrda/otkazivanje brišu hold i njegove indekse.

### 3.6. API endpoint-i

Ruta tokena `[controller]` uklanja sufiks `Controller`. `BookingsController` se nalazi u fajlu `Controllers/BookingController.cs` i zato koristi `/api/bookings`. Kod ne navodi globalni authorization fallback; tabela navodi eksplicitne zahtjeve.

| Metoda i ruta | Controller/metoda; namjena | Request → response | Pristup |
|---|---|---|---|
| `POST /api/auth/register` | `AuthController.Register`; registracija customer-a | `RegisterCustomerRequest` → token response (`201`) | Bez eksplicitnog `[Authorize]` |
| `POST /api/auth/login` | `AuthController.Login` | `LoginRequest` → token response (`200`) | Bez eksplicitnog `[Authorize]` |
| `POST /api/auth/refresh` | `AuthController.Refresh` | `RefreshTokenRequest` → novi access/refresh token | Bez eksplicitnog `[Authorize]` |
| `POST /api/auth/logout` | `AuthController.Logout` | `RefreshTokenRequest` (body može biti null) → `204` | Bez eksplicitnog `[Authorize]` |
| `POST /api/auth/forgot-password` | `AuthController.ForgotPassword` | `ForgotPasswordRequest` → generički rezultat | `[AllowAnonymous]` |
| `POST /api/auth/verify-reset-code` | `AuthController.VerifyResetCode` | `VerifyResetCodeRequest` → `204` | `[AllowAnonymous]` |
| `POST /api/auth/reset-password` | `AuthController.ResetPassword` | `ResetPasswordRequest` → `204` | `[AllowAnonymous]` |
| `POST /api/bookings/hold` | `BookingsController.Create`; kreira Redis hold | `CreateBookingRequest` → `BookingHoldResponse`, `201` | Customer |
| `POST /api/bookings/{holdToken:guid}/confirm` | `BookingsController.Confirm` | token u ruti → `BookingResponse`, `201` | Customer |
| `GET /api/bookings/{id}` | `BookingsController.GetById`; detalji customer booking-a | ID → `BookingDetailsResponse` | Customer; servis provjerava vlasništvo |
| `GET /api/bookings/my` | `BookingsController.GetMyBookings` | `GetBookingsQuery` → `PagedResponse<BookingSummaryResponse>` | Customer |
| `DELETE /api/bookings/hold/{holdToken:guid}` | `BookingsController.CancelHold` | token → `204` | Customer |
| `POST /api/bookings/{id}/cancel` | `BookingsController.CancelBooking` | ID → `204` | Customer, Employee ili Admin; servis provjerava vlasništvo/ulogu |
| `POST /api/bookings/{id}/complete` | `BookingsController.CompleteBooking` | ID → `204` | Employee ili Admin |
| `GET /api/bookings/employee` | `BookingsController.GetEmployeeBookings` | `GetBookingsQuery` → paginirani `EmployeeBookingSummaryResponse` | Employee ili Admin |
| `GET /api/bookings/employee/{id}` | `BookingsController.GetEmployeeBookingById` | booking ID → `EmployeeBookingDetailsResponse` | Employee ili Admin; employee scope se provjerava u servisu |
| `POST /api/bookings/staff` | `BookingsController.CreateStaffBooking` | `CreateStaffBookingRequest` → `EmployeeBookingDetailsResponse`, `201` | Employee ili Admin |
| `GET /api/bookings/admin` | `BookingsController.GetAdminBookingsAsync` | `GetAdminBookingsQuery` → `PagedResponse<AdminBookingSummaryResponse>` | Admin |
| `GET /api/bookings/availability` | `BookingsController.GetAvailabilityAsync` | `employeeId`, `serviceIds`, `date` → `BookingAvailabilityResponse` | Bilo koji autentifikovani korisnik |
| `GET /api/bookings/calendar-days` | `BookingsController.GetCalendarDaysAsync` | `employeeId` → `BookingCalendarDaysResponse` | Bilo koji autentifikovani korisnik |
| `GET /api/bookings/quick-availability` | `BookingsController.GetQuickAvailability` | `serviceId` → `QuickAvailabilityResponse` | Bez eksplicitnog `[Authorize]` |
| `GET /api/bookings/bookable-employees` | `BookingsController.GetBookableEmployees` | ponovljeni `serviceIds` query parametri → lista employee-a | Customer |
| `POST /api/BookableServices` | `BookableServicesController.Create` | `CreateServiceRequest` → `ServiceResponse`, `201` | Bez eksplicitnog `[Authorize]` |
| `GET /api/BookableServices/{id}` | `GetById` | ID → `ServiceResponse` | Bez eksplicitnog `[Authorize]` |
| `GET /api/BookableServices` | `GetAll` | `GetServicesQuery` → `PagedResponse<ServiceResponse>` | Bez eksplicitnog `[Authorize]` |
| `PUT /api/BookableServices/{id}` | `Update` | `UpdateServiceRequest` → `ServiceResponse` | Bez eksplicitnog `[Authorize]` |
| `DELETE /api/BookableServices/{id}` | `Delete`; deaktivira servis | ID → `204` | Bez eksplicitnog `[Authorize]` |
| `POST /api/BookableServices/{id}/activate` | `Activate` | ID → `ServiceResponse` | Bez eksplicitnog `[Authorize]` |
| `POST /api/Employees` | `EmployeesController.Create` | `CreateEmployeeRequest` → `EmployeeResponse`, `201` | Admin |
| `GET /api/Employees` | `GetAll` | `GetEmployeesQuery` → paginirani employee-i | Bez eksplicitnog `[Authorize]` |
| `GET /api/Employees/{id}` | `GetById` | ID → `EmployeeResponse` | Bez eksplicitnog `[Authorize]` |
| `PUT /api/Employees/{id}` | `Update` | `UpdateEmployeeRequest` → `EmployeeResponse` | Bez eksplicitnog `[Authorize]` |
| `DELETE /api/Employees/{id}` | `Delete`; deaktivira employee-a | ID → `204` | Bez eksplicitnog `[Authorize]` |
| `POST /api/Employees/{id}/activate` | `Activate` | ID → `EmployeeResponse` | Bez eksplicitnog `[Authorize]` |
| `GET /api/Employees/me` | `GetMyProfile` | token identitet → `EmployeeResponse` | Employee |
| `POST /api/employees/{employeeId}/services/{serviceId}` | `EmployeeServiceController.Assign` | ID-jevi u ruti → assignment response | Bez eksplicitnog `[Authorize]` |
| `GET /api/employees/{employeeId}/services` | `GetServiceEmployees` (ime metode je historijski neintuitivno; ruta je za employee assignment-e) | employee ID → assignments | Bez eksplicitnog `[Authorize]` |
| `GET /api/employees/{employeeId}/services/{serviceId}` | `EmployeeServiceController.Get` | dva ID-a → assignment | Bez eksplicitnog `[Authorize]` |
| `DELETE /api/employees/{employeeId}/services/{serviceId}` | `EmployeeServiceController.Delete` | ID-jevi → `204`/deaktivacija | Bez eksplicitnog `[Authorize]` |
| `GET /api/service/{serviceId}/employees` | `ServiceEmployeesController.GetServiceEmployees` | service ID → employee-i koji pružaju uslugu | Bez eksplicitnog `[Authorize]` |
| `POST /api/employees/{employeeId}/working-hours` | `EmployeeWorkingHoursController.Create` | `CreateEmployeeWorkingHoursRequest` → response, `201` | Bez eksplicitnog `[Authorize]` |
| `GET /api/employees/{employeeId}/working-hours` | `GetByEmployeeId` | employee ID → intervali | Bez eksplicitnog `[Authorize]` |
| `GET /api/employees/{employeeId}/working-hours/{workingHoursId}` | `GetById` | ID-jevi → interval | Bez eksplicitnog `[Authorize]` |
| `PUT /api/employees/{employeeId}/working-hours/{workingHoursId}` | `Update` | `UpdateEmployeeWorkingHoursRequest` → response | Bez eksplicitnog `[Authorize]` |
| `DELETE /api/employees/{employeeId}/working-hours/{workingHoursId}` | `Delete` | ID-jevi → `204` | Bez eksplicitnog `[Authorize]` |
| `GET /api/employees/me/availability-blocks` | `EmployeeUnavailabilityController.GetMine` | token korisnika → break/vacation lista | Employee ili Admin |
| `GET /api/employees/me/availability-blocks/{id}` | `GetById` | block ID → response | Employee ili Admin |
| `POST /api/employees/me/availability-blocks` | `Create` | `SetEmployeeUnavailabilityRequest` → response, `201` | Employee ili Admin |
| `PUT /api/employees/me/availability-blocks/{id}` | `Update` | request → response | Employee ili Admin |
| `DELETE /api/employees/me/availability-blocks/{id}` | `Delete` | block ID → `204` | Employee ili Admin |
| `GET /api/profile/me` | `ProfileController.GetMyProfile` | token korisnika → `MyProfileResponse` | Customer (kontroler-level) |
| `PATCH /api/profile/me` | `UpdateMyProfile` | `UpdateMyProfileRequest` → response | Customer |
| `POST /api/profile/me/change-password` | `ChangePassword` | `ChangePasswordRequest` → `204` | Customer |
| `DELETE /api/profile/me` | `DeleteMyAccount` | `DeleteMyAccountRequest` with current password → `204`; anonymizes history and cancels upcoming bookings | Customer |
| `POST /api/salons` | `SalonsController.Create` | `CreateSalonRequest` → `SalonResponse`, `201` | Bez eksplicitnog `[Authorize]` |
| `GET /api/salons/{salonId}` | `GetById` | salon ID → `SalonResponse` (uključuje `Phone`) | Bez eksplicitnog `[Authorize]` |
| `GET /api/booking-test/overlap` | `BookingTestController.HasOverlap` | employeeId, startsAt, endsAt → bool | Nema auth/environment zaštite u akciji |

`AppointmentsController` u `Controllers/AppointmentsController.cs` ima stare akcije unutar komentara, pa **ne pruža aktivne `/api/appointments` rute**. Stari `AppointmentService` i prateći DTO/repository kod postoje paralelno, ali novi Mobile tok koristi `/api/bookings`.

**Autorizacijska napomena:** kontroleri `EmployeesController`, `BookableServicesController`, `EmployeeWorkingHoursController`, `EmployeeServiceController`, `ServiceEmployeesController` i `SalonsController` nemaju class-level auth; većina njihovih CRUD akcija nema action-level `[Authorize]`. `Program.cs` nema fallback auth policy. Prema tome te rute trenutno ne zahtijevaju JWT samo na osnovu dostupne konfiguracije. Ovo je nalaz koda, ne test penetracije; prije izlaganja sistema izvan lokalnog okruženja treba provjeriti je li to namjeravano.

### 3.7. DTO primjeri i ugovori

Customer request za hold koristi `CreateBookingRequest` (`EmployeeId`, `StartsAt`, `ServiceIds`, opcionalni `Notes`):

```json
{
  "employeeId": 2,
  "startsAt": "2026-10-12T08:00:00Z",
  "serviceIds": [3, 5],
  "notes": ""
}
```

Staff booking koristi `CreateStaffBookingRequest`; `EmployeeId` je opcionalan u DTO-u, ali je obavezan za Admin, dok Employee može poslati samo svoj ID ili ga izostaviti:

```json
{
  "customerFirstName": "Aida",
  "customerLastName": "Kovac",
  "employeeId": 2,
  "startsAt": "2026-10-12T08:00:00Z",
  "serviceIds": [3, 5],
  "notes": null
}
```

`GET /api/bookings/calendar-days?employeeId=2` vraća:

```json
{
  "employeeId": 2,
  "from": "2026-09-01",
  "to": "2027-01-15",
  "workingDates": ["2026-09-01", "2026-09-02"]
}
```

Datumi gornjeg primjera služe samo za ilustraciju stvarne strukture (`BookingCalendarDaysResponse`); konkretan raspon zavisi od datuma, rasporeda zaposlenika i godišnjih odmora. API JSON koristi camelCase, a enum-i stringove.

`SalonResponse` sadrži `Id`, `Name`, `Address`, `City`, nullable `Phone`, `InstagramUrl`, `FacebookUrl`, koordinate, `IsActive`, `WorkingHours`. Response za profil korisnika (`MyProfileResponse`) je zaseban model sa `FirstName`, `LastName`, `Email`, `Phone`.

### 3.8. Ostali servisni dijelovi

- `AuthService`, `AccessTokenService`, `RefreshTokenService`, `AuthRepository`: registracija customer-a, provjera lozinke, JWT issuance, refresh token hash/rotation i logout/revocation.
- `PasswordResetService` i `PasswordResetCodeRepository`: verifikacijski kodovi za reset lozinke; API izlaže tri anonymous reset rute.
- `EmployeeWorkingHoursService` i `EmployeeUnavailabilityService`: provjera vlasništva/aktivnog employee-a i validacija rasporeda/izostanaka.
- `SalonService`: kreiranje i dohvat salona; nema update endpointa u pregledanom controlleru.
- `MailjetEmailService`: slanje emaila preko Mailjet HTTP API-ja.
- Nema registrovanih API background service-a. Redis hold hash ključevi imaju TTL; hold tokeni uklanjaju se iz indeksa pri brisanju holda ili pri kreiranju narednog holda za istog zaposlenika.

## 4. Mobile projekat

### 4.1. Tehnologije, pokretanje i registracija

- `AppointmentBooking.Mobile/AppointmentBooking.Mobile.csproj`: .NET MAUI Single Project; cilja `net10.0-android`, a na ne-Linux sistemima dodaje i iOS/MacCatalyst; Windows uslovno cilja `net10.0-windows10.0.19041.0`.
- Paketne verzije: `Microsoft.Maui.Controls 10.0.101`, `CommunityToolkit.Mvvm 8.4.2`, `CommunityToolkit.Maui 15.0.1`, `Microsoft.Extensions.Logging.Debug 10.0.0`.
- Android minimal API level 21; iOS 15; Windows minimum 10.0.17763. XAML source generation je uključen (`MauiXamlInflator=SourceGen`).
- Ulazna tačka je `MauiProgram.CreateMauiApp()`, aplikacija i Shell su `App.xaml.cs` i `AppShell.xaml.cs`.
- `MauiProgram.cs` DI registruje Views, ViewModels, `IApiClient`, `IAuthApiClient`, `ISessionService`, `ITokenStorage`, greške i `IBookingCalendarService`.
- Debug BaseUrl je `https://10.0.2.2:7236/` za Android emulator i `https://localhost:7236/` za ostale platforme. Android Debug handler prihvata lokalni `CN=localhost` certifikat. Release trenutno baca `InvalidOperationException` jer produkcijski API URL nije konfigurisan.
- Za Android emulator lokalni host API-ja mora biti dostupan preko emulator gateway adrese i HTTPS certifikat mora biti prihvaćen. `AndroidManifest.xml` traži `INTERNET` i `ACCESS_NETWORK_STATE`.

### 4.2. Folderi i navigacija

| Folder | Namjena |
|---|---|
| `Views/` | XAML ekrani i prateći code-behind. |
| `ViewModels/` | UI state, MVVM komande i API pozivi. |
| `Models/` | Request/response modeli koji odgovaraju API JSON-u. |
| `Services/Api/` | HTTP klijenti, bearer handler i `ApiException`. |
| `Services/Authentication/`, `Session/`, `Storage/` | Prijava, refresh sesije i sigurno čuvanje refresh tokena. |
| `Services/BookingCalendar/` | Singleton cache kalendarskih radnih dana. |
| `Configuration/` | `ApiOptions` BaseUrl. |
| `Resources/Styles/` | boje i zajednički stilovi. |
| `Platforms/` | Android/iOS/MacCatalyst/Windows bootstrap i manifesti. |

`AppShell` pri startu otvara Loading, obnavlja sesiju i na osnovu role iz JWT-a bira customer ili staff navigaciju. Customer tabs: Početna, Rezerviši, Termini, Galerija, Profil. Staff/admin tabs: Termini i Panel. `change-password`, `register` i `forgot-password` su Shell rute.

### 4.3. Ekrani i ključni fajlovi

| Ekran | View / ViewModel | Ponašanje i veze |
|---|---|---|
| Prijava | `Views/Auth/LoginPage.xaml`, `ViewModels/Auth/LoginViewModel.cs` | Prijava preko `AuthService`; prazan unos validira lokalno, greške prikazuje korisniku. |
| Registracija | `Views/Auth/RegisterPage.xaml`, `ViewModels/Auth/RegisterViewModel.cs` | Ime, prezime, email, telefon, lozinka; šalje `/api/auth/register`. |
| Zaboravljena lozinka | `Views/Auth/ForgotPasswordPage.xaml`, `ViewModels/Auth/ForgotPasswordViewModel.cs` | Email → kod → nova lozinka; koristi tri password reset rute. |
| Početna | `Views/Home/HomePage.xaml`, `ViewModels/Home/HomeViewModel.cs` | Lista usluga, quick availability, hold/confirm rezervacija. |
| Rezerviši | `Views/Booking/BookingPage.xaml`, `ViewModels/Booking/BookingViewModel.cs` | Više usluga, zaposlenik, radni kalendar, slobodni slotovi, hold i confirm. |
| Moji termini | `Views/Appointments/AppointmentsPage.xaml`, `ViewModels/Appointment/AppointmentViewModel.cs` | Upcoming, previous i cancelled sekcije; detalji u modalnom `AppointmentDetailsPage`; customer može otkazati. |
| Detalj customer termina | `Views/Appointments/AppointmentDetailsPage.xaml` | Prikazuje odgovor `BookingDetailsResponse`; modalni povratak zadržava učitano stanje liste. |
| Galerija | `Views/Gallery/GalleryProfile.xaml` | Galerijski tab; u pregledanom Mobile modelu nema API servis za galeriju, pa API integracija nije potvrđena. |
| Profil | `Views/Profile/ProfilePage.xaml`, `ViewModels/Profile/ProfileViewModel.cs` | Customer profil (`/api/profile/me`), salon (`/api/salons/{SalonId}`), radno vrijeme i Instagram; broj telefona salona je u trenutnom radnom stablu prikazan u salon sekciji. |
| Promjena lozinke | `Views/Profile/ChangePasswordPage.xaml`, `ViewModels/Profile/ChangePasswordViewModel.cs` | Zaštićeni POST `/api/profile/me/change-password`. |
| Staff termini | `Views/Staff/StaffAppointmentsPage.xaml`, `.xaml.cs`, `ViewModels/Staff/StaffAppointmentViewModel.cs` | Staff/admin kalendar, budući/past termini, odvojeni cancelled panel, detalji i staff booking kreiranje. Pri vraćanju se bira naredni datum iz radnih datuma. |
| Staff detalj termina | `Views/Staff/StaffBookingDetailsPage.xaml`, `ViewModels/Staff/StaffAppointmentViewModel.cs` | Dohvat detalja preko employee booking endpointa. |
| Staff panel | `Views/Staff/StaffPanelPage.xaml`, `.xaml.cs`, `ViewModels/Staff/StaffAvailabilityViewModel.cs` | CRUD pauza i godišnjih kroz `availability-blocks`; odjava preko sesije. |
| Loading | `Views/Startup/LoadingPage.xaml` | Privremeni ekran dok `AppShell` obnavlja token. |

Komponente koriste `ObservableObject`, `[ObservableProperty]` i `[RelayCommand]` iz CommunityToolkit.Mvvm. UI dizajn je XAML sa zajedničkim paletama u `Resources/Styles/Colors.xaml`; osnovni stilovi su u `Styles.xaml`.

### 4.4. HTTP klijenti, sesija i greške

- `IApiClient`/`ApiClient` podržavaju GET, POST, PUT, PATCH i DELETE; JSON je camelCase, enum-i se tretiraju kao stringovi. Neuspješni statusi bacaju `ApiException` sa HTTP statusom i body-em.
- `IAuthApiClient`/`AuthApiClient` zasebno pozivaju login/register/refresh/logout bez bearer handlera.
- `BearerTokenHandler` prikači trenutni access token, na prvi `401` poziva `ISessionService.RefreshAsync`, klonira zahtjev i ponavlja ga samo jednom.
- `SessionService` drži access token/role u memoriji i refresh token/expiry u `SecureStorage` preko `SecureTokenStorage`. Pri startu Shell poziva `RestoreSessionAsync`, koje pokušava refresh. Role čita iz JWT payload-a.
- `ErrorHandler` centralizuje dijalog za API/generičke greške. Loading/busy state je uglavnom u ViewModel-ima. Nisu svi API pozivi potpuno centralizovani kroz repository sloj; ViewModel-i direktno koriste `IApiClient`.
- `IBookingCalendarService` je singleton i cacheira `BookingCalendarDaysResponse` u `ConcurrentDictionary` po employee ID-u. U okviru aktivnog procesa drugi panel/tab ne traži ponovo endpoint; nema invalidacije cache-a u pregledanom servisu. Novi proces aplikacije kreira novi cache.

### 4.5. API pozivi iz Mobile aplikacije

| Mobile funkcija i poziv | API ugovor | Integracija |
|---|---|---|
| `AuthService.LoginAsync` / `RegisterAsync` | `POST /api/auth/login`, `/register`; `LoginRequest` / `RegisterRequest` ↔ auth request DTO; token response | Strukture polja se podudaraju prema pregledanim modelima; odgovor se sprema u sesiju. |
| `AuthService` password reset | `POST /api/auth/forgot-password`, `verify-reset-code`, `reset-password` | Request modeli postoje na obje strane; Mobile obrađuje API greške. |
| `HomeViewModel` usluge i prvi slot | `GET /api/BookableServices?isActive=true&page=1&pageSize=20`, `GET /api/bookings/quick-availability?serviceId=...` | JSON lista usluga i quick slotovi. Quick endpoint je javno dostupan u API anotacijama; Mobile ga poziva bez posebnog auth uslova u kodu. |
| `HomeViewModel` rezervacija | `POST /api/bookings/hold`, `POST /api/bookings/{holdToken}/confirm`, `DELETE /api/bookings/hold/{holdToken}` | Hold i confirmation su Customer-only; odgovori se čitaju kao hold/booking DTO-i. |
| `BookingViewModel` usluge/zaposlenici | `GET /api/BookableServices?...`, `GET /api/bookings/bookable-employees?serviceIds=...` | Više ID-jeva usluga se šalje u query-u, employee lista odgovara izboru usluga. |
| `BookingViewModel` datumi/slotovi | `GET /api/bookings/calendar-days?employeeId=...`, `GET /api/bookings/availability?employeeId=...&serviceIds=...&date=...` | Calendar response model isti je oblikom: `EmployeeId`, `From`, `To`, `WorkingDates`; availability response `Slots`. |
| `AppointmentViewModel` lista/detalj | `GET /api/bookings/my?page=1&pageSize=50`, `GET /api/bookings/{id}` | `PagedResponse<BookingSummaryResponse>` / `BookingDetailsResponse`; Customer-only i servis ograničava detalj na vlasnika. |
| `AppointmentViewModel` cancel | `POST /api/bookings/{id}/cancel` | API dozvoljava Customer/Employee/Admin; Mobile customer koristi. |
| `ProfileViewModel` customer profile | `GET/PATCH /api/profile/me` | `MyProfileResponse` i `UpdateMyProfileRequest`; profil endpoint je Customer-only. |
| `ProfileViewModel` salon info | `GET /api/salons/1` (glavni projekat) | API `SalonResponse` trenutno ima nullable `Phone`; Mobile model u radnom stablu takođe ima `Phone`, a XAML binduje `Salon.Phone`. `SalonId` je hardkodiran, nije dobavljen preko API konfiguracije. |
| `ChangePasswordViewModel` | `POST /api/profile/me/change-password` | Customer-only, `204` očekivani odgovor. |
| `StaffAvailabilityViewModel` | GET/POST/PUT/DELETE `/api/employees/me/availability-blocks` | API i Mobile request/response nose `Type`, `StartsAt`, `EndsAt`, `IsAllDay`, `Note`; enum vrijednosti u API-ju su `Break=1`, `Vacation=2`, JSON enum je string. |
| `StaffAppointmentViewModel` dohvat identiteta | `GET /api/employees/me`; admin lista `GET /api/employees?isActive=true&page=1&pageSize=100` | Employee ruta je Employee-only; list ruta nema eksplicitnu auth anotaciju. |
| `StaffAppointmentViewModel` employee services | `GET /api/employees/{employeeId}/services` | API endpoint vraća assignment-e; Mobile koristi da ograniči dostupne usluge. |
| `StaffAppointmentViewModel` termini/detalji | `GET /api/bookings/employee?...`, `GET /api/bookings/employee/{bookingId}` | Employee/Admin; Employee dobija samo svoje, Admin servis koristi admin pretragu. |
| `StaffAppointmentViewModel` ručni booking | `POST /api/bookings/staff` | Employee/Admin; Mobile request odgovara `CreateStaffBookingRequest`; neregistrovani customer se čuva kao ime/prezime direktno na booking zapisu. |

Mobile ne poziva aktivne stare `/api/appointments` rute. Nema pronađene Mobile integracije za service CRUD, employee CRUD, working-hours CRUD, salon kreiranje ili admin booking filter endpoint. Email i Redis test kontroleri su uklonjeni.

## 5. Tokovi kroz oba projekta

### Customer kreira rezervaciju

1. Korisnik u `BookingPage` bira usluge, zaposlenika, datum i slot.
2. `BookingViewModel` učitava usluge, `/bookings/calendar-days`, bookable employees i `/bookings/availability`.
3. Odabrani slot ide u `POST /api/bookings/hold` sa `CreateBookingRequest`.
4. `BookingsController.Create` poziva `BookingService.CreateHoldAsync`; servis validira schedule, a `BookingHoldRepository` pravi Redis hold.
5. Mobile zatim poziva `POST /api/bookings/{holdToken}/confirm`.
6. `BookingService.ConfirmAsync` upisuje booking i appointment stavke unutar PostgreSQL transakcije; API vraća `BookingResponse`.
7. `BookingViewModel` prikazuje potvrdu i nakon toga lista termina koristi `/api/bookings/my`.

### Staff ručno unosi booking bez naloga

1. Staff/admin koristi formu iz staff termina panela.
2. Mobile šalje `CreateStaffBookingRequest` na `POST /api/bookings/staff`.
3. Employee može rezervisati samo za sebe; Admin bira employee ID.
4. API validira usluge, radno vrijeme, pauzu/godišnji, zakazani overlap i Redis hold.
5. `BookingRepository.CreateAsync` atomarno upisuje booking i appointments. Korisnik bez naloga se ne kreira u `customers`; koriste se `customer_first_name`, `customer_last_name` i `customer_id = NULL`.
6. Mobile prima `EmployeeBookingDetailsResponse` i osvježava staff prikaz.

### Staff upravlja pauzama/godišnjim

`StaffAvailabilityViewModel` šalje CRUD zahtjeve na `/api/employees/me/availability-blocks`. API identificira trenutnog employee-a iz JWT-a, validira razliku između Break i Vacation te čuva intervale u `employee_unavailability`. `BookingService.GetAvailabilityAsync` filtrira slotove koji se vremenski preklapaju s blokovima. `GetCalendarDaysAsync` trenutno izbacuje samo all-day Vacation datume iz date list-e; Break se rješava na availability slot nivou.

### Customer pregledava salon i profil

Profil učitava `/api/profile/me`, zatim `/api/salons/{SalonId}`. Model telefona salona je nullable; Mobile prikazuje fallback tekst kada API vrati null. Oba endpointa su odvojeni: `MyProfileResponse.Phone` je customer telefon, a `SalonResponse.Phone` je salon telefon.

## 6. Trenutni razvoj, problemi i ograničenja

### Potvrđeno implementirano

- Customer auth, JWT, refresh token rotacija i reset lozinke.
- Katalog usluga, employee assignments i radni sati.
- Multi-service booking, availability, privremeni hold, potvrda, customer lista/detalji/otkazivanje.
- Employee unavailability za break/vacation i staff/admin booking operacije.
- Walk-in booking s customer imenom/prezimenom bez account zapisa.
- Staff/admin Mobile paneli i kalendar datuma.
- Salon profil i salon telefon u trenutnom radnom stablu.
- Dovršavanje termina iz staff panela.

### Djelimično implementirano ili zastarjelo

- `AppointmentsController` ima komentarisane stare akcije; aktivni tok je `BookingsController`.
- `README.md` ima historijski opis i sadrži zastarjele tvrdnje: npr. expired hold cleanup opisuje kao aktivan DB cleanup, buduće funkcionalnosti navodi auth/availability, i koristi starije `Appointments` konfiguracijske nazive. Kod u `Program.cs` i `BookingOptions` je mjerodavan.
- Migration 014 za salon telefon je prisutna kao necommitovan fajl; Compose je ne montira. Isto važi za migration 013 walk-in customer kolone.
- Release API URL u Mobile aplikaciji nije konfigurisan; Release kreiranje DI konfiguracije trenutno baca iznimku.
- `SalonId` je hardkodiran (`1` u glavnom Mobile projektu; testna kopija koristi `4`). To može pogrešno povezati salon na drugoj bazi.
- `BookingCalendarService` cache je process-local i ne invalidira se; API promjene rasporeda/odmora tokom istog procesa mogu ostati nevidljive do novog procesa.
- Availability koristi `TimeZoneInfo.Local`; zone baze/hosta/salona nisu eksplicitno standardizovane u pregledanom kodu.
- Brojne upravljačke API rute nemaju `[Authorize]` i globalni fallback ne postoji. Treba potvrditi namjeru i pristupnu granicu prije javnog deploymenta.
- `BookingTestController` nema auth/environment guard.
- `SalonResponse.FacebookUrl` ostaje u API/Mobile DTO-ima, iako Mobile profil više nema Facebook UI u trenutnom radnom stablu. Sam API response contract i salon DB kolona još sadrže Facebook.

### Git stanje i nedavne promjene

Posljednji commit poruke su `add staff panels with appointments overview and implement feature of adding appointment and breaks` u oba repozitorija, uz različite grane/commit hash-e. Trenutne promjene nisu commitovane. API worktree mijenja booking controller/service, salon DTO/model/repository/service, employee working-hours service i dodaje `GetBookingCalendarDaysQuery`, response i SQL `014_add_phone_to_salons.sql`. Mobile worktree mijenja staff booking/calendar, booking/calendar UX, Maui DI i profil salon telefona; dodaje calendar DTO i `Services/BookingCalendar`. Git log sam po sebi ne potvrđuje da su ove izmjene dovršene niti primijenjene u stvarnoj bazi.

### Otvorena pitanja koja se ne mogu riješiti iz koda

1. Da li su SQL skripte 013 i 014 stvarno primijenjene na bazu koju koristi aplikacija? Compose trenutno ne primjenjuje njih.
2. Koja je kanonska vremenska zona salona i kako je API host konfiguriše?
3. Da li je namjerno da employee, service, working-hours, service-assignment i salon CRUD većinom nemaju authorization?
4. Koja produkcijska API adresa treba biti postavljena za Mobile Release?
5. Da li salon ID mora biti konfigurabilan umjesto hardkodiranog ID-a?
6. Da li se API i baza trenutno pokreću iz IDE-a, Dockera ili drugog okruženja, i da li postoji zasebna test baza?
7. Da li se brisanje/deaktivacija booking/service/employee podataka očekuje kao soft delete ili trajno uklanjanje u svim slučajevima?
8. Da li su staff/admin role i jedini employee modeli za buduće instalacije ili su samo trenutno seed/test okruženje?

## 7. Pokretanje i provjere

### API

Potrebni su .NET SDK 10, PostgreSQL 18 i Redis. Lokalni API:

```powershell
dotnet restore AppointmentBooking.Api.slnx
dotnet run --project AppointmentBooking.Api/AppointmentBooking.Api.csproj
```

Uslovi konfiguracije: `ConnectionStrings:Database`, `Jwt` sekcija i `Redis:ConnectionString`; Mailjet vrijednosti su potrebne kada se koristi email servis. Swagger je samo u Development okruženju. Docker način je `docker compose up --build`, ali trenutno Compose nema mount skripti 013/014.

### Mobile

Potrebni su .NET 10 SDK, .NET MAUI workload i Android SDK/emulator za Android. Glavni Android build:

```powershell
dotnet restore AppointmentBooking.Mobile.slnx
dotnet build AppointmentBooking.Mobile/AppointmentBooking.Mobile.csproj -f net10.0-android
```

Android emulator Debug BaseUrl je `https://10.0.2.2:7236/`; API mora biti pokrenut lokalno i HTTPS certifikat dostupan. iOS build zahtijeva odgovarajući Apple/Xcode host/toolchain; nije provjeren.

### Testovi

API testovi su xUnit/Moq/FluentAssertions. Unit testovi obuhvataju `AppointmentService`, `BookableService`, `EmployeeService`, `EmployeeWorkingHoursService` i validaciju working hours DTO-a. Integration testovi u `Integration/` šalju HTTP zahtjeve aplikaciji i rade upise/deaktivacije employee/work-hours podataka. `IntegrationTestWebApplicationFactory` navodi lokalnu PostgreSQL konfiguraciju. Testovi nisu pokrenuti tokom ove analize jer bi integration testovi mijenjali bazu, a sigurna izolovana test baza nije potvrđena. `UnitTest1.cs` je prazan/placeholder test.

### Stvarno izvršene provjere

- Pregledani su API/Mobile folderi, kontroleri, servisi, repozitoriji, SQL skripte, DTO-i, DI/config, Mobile ViewModel-i, XAML rute, testovi i Git status.
- `dotnet build AppointmentBooking.Api.slnx --no-restore` nije prošao: solution uključuje i `../../Test/PasswordHashGenerator/PasswordHashGenerator.csproj`; tokom direktnog API builda kopiranje output DLL/EXE nije uspjelo jer je fajl koristio drugi proces (API je vjerovatno već bio pokrenut). Izlaz sadrži i NU1903 upozorenje za `Microsoft.OpenApi 2.0.0` u tom dodatnom projektu te API warnings; build ne potvrđuje čist uspjeh.
- Android build glavnog Mobile projekta (`dotnet build ... --framework net10.0-android --no-restore`) završio je statusom `Build FAILED` bez prikazane compiler greške (0 errors); DLL je bio generisan. Zbog kontradiktornog exit statusa rezultat se ne označava kao uspješan build.
- Android build pomoćne kopije `C:\src\AppointmentBooking.Mobile` je u ranijoj provjeri prošao, ali ona nije kanonski projekat i build nije ponovljen za ovaj dokument.
- Nisu pokrenuti API testovi, aplikacija, emulator, stvarni HTTP zahtjevi, migracije niti pristup bazi. Nije potvrđeno runtime ponašanje.

Build komande mogu ažurirati generisane `bin/obj` artefakte. Tokom zadatka nisu namjerno mijenjani izvorni kod, konfiguracija, SQL skripte, baze ili zavisnosti. Dokument je jedini namjerno kreiran fajl.

## 8. Pravila za budućeg AI asistenta

1. Komunicirati na bosanskom jeziku; nazive koda, tabela, DTO-a i ruta zadržati originalne.
2. Prije izmjene pročitati stvarni API i Mobile kod, uključujući trenutne necommitovane izmjene.
3. API i Mobile tretirati kao jedan sistem. Prije promjene request/response DTO-a provjeriti oba kraja.
4. Zadržati postojeću Controller → Service → Repository arhitekturu API-ja i postojeći MAUI MVVM pristup.
5. Mijenjati samo fajlove potrebne za konkretan zahtjev; ne refaktorisati povezane ili nepovezane dijelove bez razloga.
6. Ne uvoditi EF Core, Minimal API, nove biblioteke ili novu arhitekturu bez odobrenja i opravdanja.
7. Ne mijenjati bazu/migracije ili API contract bez prethodne provjere Mobile potrošača i odobrenja korisnika.
8. Prije višeslojne promjene predložiti tačan pristup i sačekati korisnikovo odobrenje.
9. Poslije implementacije navesti tačne fajlove, API rute/JSON primjere ako su contract mijenjani, build/test rezultate i ono što nije moglo biti provjereno.
10. Ne kopirati tajne vrijednosti iz konfiguracije, logova ili testnih fajlova u dokumente ili odgovore.
11. Ne pokretati integration testove koji pišu u bazu dok izolovana test baza nije potvrđena.
12. Provjeriti da li se rad radi u glavnoj Mobile kopiji ili pomoćnoj Android kopiji; ove kopije mogu odstupati.

## 9. Najvažniji fajlovi za buduće izmjene

### API

- `AppointmentBooking.Api/Program.cs` — DI, HTTP pipeline, JWT, CORS, OpenAPI i infrastruktura.
- `AppointmentBooking.Api/Controllers/BookingController.cs` — aktivne booking rute.
- `AppointmentBooking.Api/Services/Bookings/BookingService.cs` — booking, availability, calendar-days, walk-in, cancel/complete logika.
- `AppointmentBooking.Api/Repositories/Bookings/BookingRepository.cs` — booking SQL, transakcije i pretrage.
- `AppointmentBooking.Api/Repositories/BookingHolds/BookingHoldRepository.cs` — Redis holdovi.
- `AppointmentBooking.Api/Services/EmployeeUnavailability/EmployeeUnavailabilityService.cs` i `Repositories/EmployeeUnavailability/EmployeeUnavailabilityRepository.cs` — pauze/godišnji.
- `AppointmentBooking.Api/DTOs/Bookings/` — API booking contract.
- `AppointmentBooking.Api/Controllers/ProfileController.cs`, `Services/Profile/ProfileService.cs` — customer profil.
- `AppointmentBooking.Api/Controllers/SalonsController.cs`, `DTOs/Salons/Response/SalonResponse.cs`, `Repositories/Salons/SalonRepository.cs` — salon podaci i telefon.
- `AppointmentBooking.Api/Database/001_schema.sql` do `014_add_phone_to_salons.sql` — shema; potvrditi stvarno stanje DB-a prije svake izmjene.
- `compose.yaml` — baze i skripte koje se zaista mountaju.

### Mobile

- `AppointmentBooking.Mobile/MauiProgram.cs` i `AppShell.xaml.cs` — DI, BaseUrl, sesija i navigacija.
- `AppointmentBooking.Mobile/Services/Api/ApiClient.cs`, `AuthApiClient.cs`, `BearerTokenHandler.cs` — HTTP i auth.
- `AppointmentBooking.Mobile/Services/Session/SessionService.cs`, `Storage/SecureTokenStorage.cs` — tokeni i lifecycle sesije.
- `AppointmentBooking.Mobile/ViewModels/Booking/BookingViewModel.cs` i `Views/Booking/BookingPage.xaml` — customer booking tok.
- `AppointmentBooking.Mobile/Services/BookingCalendar/BookingCalendarService.cs` — calendar API cache.
- `AppointmentBooking.Mobile/ViewModels/Appointment/AppointmentViewModel.cs`, `Views/Appointments/AppointmentsPage.xaml`, `AppointmentDetailsPage.xaml` — customer termini.
- `AppointmentBooking.Mobile/ViewModels/Staff/StaffAppointmentViewModel.cs`, `Views/Staff/StaffAppointmentsPage.xaml` — staff termin/kalendar.
- `AppointmentBooking.Mobile/ViewModels/Staff/StaffAvailabilityViewModel.cs`, `Views/Staff/StaffPanelPage.xaml` — pauze i godišnji.
- `AppointmentBooking.Mobile/ViewModels/Profile/ProfileViewModel.cs`, `Views/Profile/ProfilePage.xaml`, `Models/Salons/Responses/SalonResponse.cs` — profil i salon telefon.
- `AppointmentBooking.Mobile/Models/` — provjeriti svaki model kad se mijenja JSON contract.

## 10. Sažetak za AI asistenta koji nastavlja razvoj

Ovo je .NET 10 ASP.NET Core API + .NET MAUI mobilna aplikacija za rezervacije. API koristi Controller → Service → Repository slojeve, PostgreSQL kroz Dapper/Npgsql, Redis za privremene holdove i JWT role. Mobile koristi XAML/MVVM, `IApiClient`, camelCase JSON i bearer refresh handler. Customer rezervacija ide kroz availability → Redis hold → confirm → PostgreSQL transakciju. Staff booking bez naloga čuva customer ime/prezime na `bookings`, ne kreira customer account. Employee break/vacation se čuva u `employee_unavailability` i filtrira availability. Kalendarski datumi se cacheiraju u Mobile procesu.

Prije svakog zadatka provjeri trenutno radno stablo; API/Mobile imaju necommitovane promjene i pomoćna Android kopija može odstupati. Posebno provjeri da li su SQL migracije 013/014 primijenjene i da li Compose ih uključuje — trenutni Compose ih ne mounta. Ne mijenjaj API contract bez pregleda Mobile modela i poziva. Uočen je nedostatak eksplicitne autorizacije na više API CRUD ruta, a Release BaseUrl nije konfigurisan. API i Mobile build provjere u ovoj analizi nisu potvrdile čist uspjeh; integration testovi nisu pokrenuti zbog upisa u bazu.
