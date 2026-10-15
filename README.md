# Vehicle Parking System

A Web API and frontend for managing vehicle parking — authentication, parking space availability, ticket issuing, fee calculation, and payment recording.

## Features

**Authentication & Authorization**
- JWT-based login/register, with hashed passwords (ASP.NET `PasswordHasher`)
- Role-based access (Admin/User) — only Admins can issue tickets, process exits, and record payments
- Frontend route guard redirects to login if no valid session exists
- Global 401 handling — an expired or invalid token redirects to login automatically

**Parking Management**
- Slots typed by vehicle category (Car/Bike/Truck), each tracked separately
- A Singleton (`ParkingSpaceTracker`) holds live available-slot counts in memory, loaded from the database at startup, to avoid querying the database on every availability check
- Vehicle lookup/creation on ticket issue, with validation against duplicate active tickets and vehicle-type mismatches

**Tickets, Fees & Payment**
- Ticket issued on entry, fee calculated on exit (rounded up to the next hour, minimum 1-hour charge), payment recorded separately from exit calculation
- Payment method selection (Cash/Card/JazzCash) — recorded only, no payment gateway integration
- Printable ticket via the browser's print dialog

**Reliability & Concurrency**
- Global exception handling: one `IExceptionHandler` logs full details server-side and returns generic `ProblemDetails` responses, so internals are never exposed
- Issuing tickets runs in an explicit `Serializable` transaction, so two requests cannot be given the same slot (a deadlock victim receives 409)
- Exit and payment use optimistic concurrency (`RowVersion` on `ParkingTicket`), so a concurrent duplicate request receives 409 instead of silently overwriting the first
- Details and the experiments behind these choices: [TRANSACTIONS.md](TRANSACTIONS.md)

## Tech Stack

- **Backend:** ASP.NET Core Web API (.NET 10), EF Core Code First, SQL Server
- **Auth:** JWT Bearer authentication
- **Frontend:** Plain HTML/CSS/JavaScript (no framework), Fetch API
- **Secrets:** .NET User Secrets for local development (JWT signing key is not committed)

## Architecture Notes

- **Singleton pattern** — `ParkingSpaceTracker` guarantees a single, shared in-memory count of available slots per vehicle type, avoiding repeated database queries on every availability check. The source of truth remains the database; the tracker is a cache updated only after `SaveChanges` succeeds, and its operations are guarded by a `lock` so concurrent requests cannot corrupt the counts.
- **Strategy pattern** — fee calculation lives behind an `IFeeCalculator` interface (`HourlyFeeCalculator`), so new pricing rules could be added without modifying existing code (Open/Closed Principle).
- **Dependency Injection** — `TokenService`, `IPasswordHasher`, and `IFeeCalculator` are all injected via ASP.NET Core's built-in container, keeping controllers decoupled from concrete implementations (Dependency Inversion Principle).
- **Global exception handling** — `GlobalExceptionHandler` (`IExceptionHandler`, registered with `AddExceptionHandler` and enabled by `UseExceptionHandler()` as the first middleware) maps known failures to status codes: unique-constraint violations, deadlocks and concurrency conflicts return 409, and anything else returns a generic 500.

## Setup

1. Clone the repo
2. Set the JWT signing key via User Secrets:
```bash
   cd ParkingSystem
   dotnet user-secrets set "Jwt:Key" "your-secret-key-at-least-32-characters"
```
3. Update the connection string in `appsettings.json` if needed
4. Apply migrations:
```bash
   dotnet ef database update
```
5. Run the API:
```bash
   dotnet run
```
6. Open `ParkingSystem-frontend/index.html` via Live Server (or any static server)

## API Endpoints

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/api/auth/register` | — | Register a new user |
| POST | `/api/auth/login` | — | Log in, returns JWT |
| GET | `/api/parking/availability` | Any logged-in user | Available slots per vehicle type |
| POST | `/api/parking/tickets` | Admin | Issue a parking ticket (409 if the vehicle is already parked, no slot is free, or the request lost a concurrency conflict) |
| GET | `/api/parking/tickets/{id}` | Admin | Get a ticket's current state |
| PATCH | `/api/parking/tickets/{id}/exit` | Admin | Calculate exit fee (409 if already calculated or modified by a concurrent request) |
| POST | `/api/parking/tickets/{id}/pay` | Admin | Record payment, free the slot (409 if already paid or modified by a concurrent request) |

## Known Limitations

- The availability tracker is an in-memory cache: it assumes a single running app instance, and manual database edits are not reflected until the app restarts
- A request that loses a deadlock or concurrency conflict receives 409 and must be retried manually (no automatic retry)
- The frontend currently shows raw JSON for 409 responses produced by the global handler
- No refresh tokens — session simply expires after token lifetime
- Token stored in `sessionStorage`; a production system would likely use an `httpOnly` cookie to protect against XSS
- No payment gateway integration (by design, per requirements) — payment method is recorded only