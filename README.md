# Northwind Support Desk

A support ticket management system: raise tickets, search and filter them, assign them to agents,
and move them through their lifecycle.

**.NET 10 Web API** + **React 19 / TypeScript** + **SQL Server**, in one monorepo.

### DEFECT-117

The ticket list hook's fetch effect watched search and pagination fields but omitted the other
filters. Changing Status updated state without triggering a request, so the stale filter was only
sent after a watched field changed. The hook now requests immediately for non-search filters and
debounces search alone. The regression test verifies both the request parameters and timing.

### Ticket list filtering

`TicketQueries` composes exact-match and trimmed search predicates on the EF Core query before
`CountAsync`, sorting, and paging. Search checks title, reference, and customer name; supplied
filters combine with AND, and blank search is ignored. A later filter can be added as another
predicate in the same query pipeline, so it also affects the count and pages. The SQLite-backed
query test covers priority, combined filters, customer-name search, and filtered paging.
The SLA status predicate is also composed in this query before counting and paging; the API
evaluates its due/resolution boundaries and configured at-risk threshold using the injected clock.
The relational query test runs the composed query against SQLite and verifies the filtered result
and count. Development EF Core command logging is enabled for inspecting the generated SQL when
running against SQL Server; no SQL Server query plan or production-scale performance claim is made.
The list and detail views share a text-labelled SLA indicator, and changing the SLA filter triggers
an immediate list request.

### Task 2: create-time triage

Ticket creation applies the configured priority and SLA window, then assigns the active,
qualified agent with the fewest open tickets below their limit. Equal workloads are resolved by
the lower agent ID for deterministic results. Category flags drive specialist and forced-critical
behavior; no category names are hard-coded. The API returns the decision reasons with the created
ticket. SLA windows, the premium multiplier, the minimum window, and the at-risk threshold come
from the existing `Sla` configuration section.

### Ticket escalation API

`POST /api/tickets/{id}/escalate` raises priority one level, recalculates the due date from the
injected clock, and keeps the current agent when they remain active, qualified, and under capacity.
Otherwise it chooses the least-loaded eligible agent, with the same lowest-ID tie-break as create
triage, or leaves the ticket unassigned. Critical, resolved, and closed tickets return 409. The
reason is trimmed and must be 5–500 characters. The actor is recorded from the signed-in agent.

Each successful escalation and its immutable history row are saved in the same unit of work.
`GET /api/tickets/{id}/escalations` returns the rows newest first with before/after priority, owner,
and due date. `TicketEscalations` cascades when its ticket is deleted, while agent references use
restrict behavior to preserve actor/owner references. The composite index
`IX_TicketEscalations_TicketId_EscalatedAtUtc` starts with `TicketId` for the history lookup and
supports newest-first ordering. `IX_TicketEscalations_FromAgentId` and
`IX_TicketEscalations_ToAgentId` support the restrictive agent foreign keys without table scans.
The detail page validates the reason before submission, displays server conflicts, and refreshes
the ticket and history in place. Escalation history records the authenticated agent from the token;
the client cannot choose that actor.

### Task 3: API authentication

Credentials are stored on the existing `Agent` aggregate because every account in this assignment
is an agent; a separate Users table would duplicate the identity. `PasswordHash` is generated and
verified with ASP.NET Identity's salted `PasswordHasher<Agent>`. Development seeding hashes the
fixture password at seed time. Use any active seeded agent email with password
`NorthwindDev!2026` (local development only); for example,
`alex.turner@northwind-support.example` / `NorthwindDev!2026`.

`POST /api/auth/login` issues an HS256 JWT with the agent ID, name and email; tokens expire after
60 minutes. `GET /api/auth/me` returns that identity. A fallback authorization policy protects all
API controllers, with login explicitly anonymous. The signing key is read from `Jwt:SigningKey` or
`Jwt__SigningKey`; the development appsettings value is a local-only fixture and must be replaced
outside local development. The escalation actor comes from the signed name claim. The browser
stores the token in `localStorage` and sends it as a bearer token through the shared API client;
this keeps reloads signed in, with the trade-off that same-origin script injection could read it.
An expired or rejected token clears the stored session and returns the user to sign-in.

### Design choices and patterns

Three patterns fit this scope: (1) a **Query Object** (`TicketQueries`) owns composable database
filters so each new filter remains in SQL before count and paging; (2) **CQRS-style handlers** keep
state-changing commands separate from read queries without adding a mediator dependency; and (3) a
shared **triage policy service** applies the same priority, SLA, and assignment rules during create
and escalation. `IClock` makes time-boundary rules deterministic. A generic repository layer was
rejected because it would hide query composition without helping this scope.

Credentials live on `Agent` because every user in this assignment is an agent. ASP.NET Identity's
password hasher handles salted password verification. The browser uses `localStorage` to retain a
login across reloads; the XSS trade-off is described above. Docker Compose enables startup migration
only through a runtime flag for this assignment; production deployments should run migrations as a
controlled job.

### Task 4: Docker API

`apps/api/Dockerfile` restores from the SDK image before copying API source, publishes the API, then
runs it from the smaller ASP.NET runtime image as the image's non-root `app` user on HTTP port 8080.
The publish stage removes `appsettings.Development.json`; the image contains no development
configuration, connection string, or JWT key. Compose supplies the connection string using the
`sqlserver` service name, the environment name, and JWT settings at runtime. Copy `.env.example` to
`.env` before starting Compose; its credentials are development fixtures only. The API waits for
SQL Server's health check, applies migrations, and seeds demo data on startup through
`Database__ApplyMigrationsOnStartup`.

```bash
cp .env.example .env
docker compose down -v && docker compose up --build
```

The `down -v` step deletes this Compose project's SQL volume, so use it only when resetting local
demo data is acceptable. Check `http://localhost:5080/health` for database reachability. To read the
seeded tickets, sign in at `POST /api/auth/login` with the development credentials above and send its
`Token` as a bearer token to `GET /api/tickets`; application endpoints remain protected.

PowerShell example for the seeded list:

```powershell
$login = Invoke-RestMethod -Method Post -Uri http://localhost:5080/api/auth/login `
  -ContentType 'application/json' `
  -Body '{"email":"alex.turner@northwind-support.example","password":"NorthwindDev!2026"}'
Invoke-RestMethod -Uri http://localhost:5080/api/tickets `
  -Headers @{ Authorization = "Bearer $($login.token)" }
```

Run the React app against this container with `VITE_API_PROXY_TARGET=http://localhost:5080 npm run
web` (PowerShell: `$env:VITE_API_PROXY_TARGET='http://localhost:5080'; npm run web`). The proxy
keeps the browser on the Vite origin.

Compose enables migrations and demo seeding for this self-contained assignment environment. In a
real deployment, run migrations once as a controlled deployment job with a restricted database
identity before rolling out the API. Having every API replica migrate at startup can create races,
requires schema-change privileges at runtime, and couples availability to migration work.

Docker verification on Windows Docker Desktop: after `docker compose down -v`, Compose brought up
the API against an empty SQL volume; `/health` returned `Healthy` and the authenticated ticket list
returned 40 seeded tickets. After a temporary `.cs` comment change, the restore step was `CACHED`.
`docker compose exec api whoami` returned `app`. Docker Desktop reported the API image as 455 MB
disk usage / 125 MB content size and the .NET 10 SDK image as 1.3 GB disk usage / 351 MB content
size. A temporary container check also confirmed `/app/appsettings.Development.json` is absent from
the rebuilt image. These are local image-store measurements; disk usage includes shared image layers.

### Known limitations

There are no refresh tokens, account lockout or rate limiting, roles or permissions, registration,
password reset, OAuth/SSO, MFA, notifications, background jobs or real-time updates. The Task 2
optional SLA summary report is not implemented.

---

## Quick start

**Prerequisites:** .NET SDK 10 (`global.json`), Node.js 22.19 or newer (`.nvmrc`), Docker.

```bash
cp .env.example .env          # optional, every value has a working default
npm install                   # installs the web app
npm run db:up                 # SQL Server 2022 in Docker on localhost:1433
npm run api                   # terminal 1: migrates, seeds, then serves the API
npm run web                   # terminal 2: React dev server
```

| What | Where |
| --- | --- |
| Web app | http://localhost:5173 |
| API | https://localhost:7043 (also http://localhost:5043) |
| Swagger | https://localhost:7043/swagger |

In Development the API applies migrations and seeds demo data on startup. **Never commit real
credentials.**

**Visual Studio 2026:** run `npm run db:up`, open `SupportDesk.sln`, set
**SupportDesk.Presentation** as the startup project, and press F5 on the **https** profile.

## Tests

```bash
npm test                      # dotnet test, then the web tests (Vitest)
npm run typecheck --workspace apps/web
```

Backend unit and architecture tests use SQLite or test doubles; the frontend uses Vitest and React
Testing Library. These checks do not need SQL Server. Docker startup, database health, seeded tickets,
restore caching, image sizes, and the non-root user were verified separately as described above.
`Microsoft.Data.SqlClient` brings in `System.Security.Cryptography.Pkcs`; the central package file pins
it to 9.0.21, the patched 9.x release, instead of the vulnerable 9.0.11 version reported by NuGet.
Final verification on Node 22.22.2 passed: `npm test` (53 backend and 17 frontend tests),
`npm run build`, `npm run lint`, and `npm run typecheck`. The API restore completed without
vulnerability warnings after the package pin.

The `screenshots/` directory contains candidate-captured test evidence and browser views of the
login page, new-ticket form, customer list, ticket list, and ticket detail. The test screenshots
document verification; the browser screenshots show the submitted UI in use. `login.json` is a
captured development login response included to show the response shape; its JWT is replaced with a
redaction marker, so it is not a usable session. These evidence files are excluded from the Docker
build context.

### Submission details

- Fork: <https://github.com/Sithumini-Anuhansi/northwind-support>
- Development login: `alex.turner@northwind-support.example` / `NorthwindDev!2026` (local fixture only)
- Time spent: the candidate should provide a rough estimate with the submission; it was not tracked
  during implementation.

### AI usage

Codex helped inspect the repository, explain the defect and filtering flow, draft implementation and
tests, and interpret build and Docker output. The workflow that worked best was to take one acceptance
criterion at a time, trace it through the UI/API/database boundary, then review the resulting code
and test behavior before moving on. The candidate ran the tests and manual browser/API checks,
captured screenshots, and reviewed the behavior. An AI-provided PowerShell setup command was
initially run in Command Prompt and failed; it was replaced with the equivalent CMD command. Docker
Compose's progress option also produced a non-fatal flag-order suggestion, which was corrected in the
follow-up instructions. Test results in this README are based on candidate-run checks.

The composable query and shared triage-policy drafts were kept after reviewing their call paths and
behavior. A first AI-written auth-context split used filenames differing only by case; Windows
typecheck caught the collision, and the shared module was renamed. An initial ticket-hook lint fix
also failed the candidate's lint run, so the effect was revised to update state after the request
settled. The initial Node 20 test run exposed that locked `undici` requires Node 22.19 or newer; the
candidate switched to Node 22.22.2 and reran all checks successfully.

## Commands

| Command | What it does |
| --- | --- |
| `npm run db:up` / `db:down` / `db:reset` | Start / stop / wipe SQL Server |
| `npm run api` / `npm run web` | Run the API / the React app |
| `npm run build` | Build both |
| `npm run lint` / `typecheck` / `format` | Web lint and types / `dotnet format` |
| `npm run ef -- migrations add <Name>` | Add an EF Core migration |

## API at a glance

| Endpoint | Description |
| --- | --- |
| `POST /api/auth/login` · `GET /api/auth/me` | Sign in / current agent |
| `GET /api/tickets` | Paged list with database-side `search`, `status`, `priority`, `categoryId`, `customerId`, `assignedAgentId`, `unassignedOnly`, `slaStatus`, `sortBy`, `sortDirection`. |
| `GET /api/tickets/{id}` | One ticket |
| `POST /api/tickets` | Create a ticket |
| `POST /api/tickets/{id}/escalate` · `GET /api/tickets/{id}/escalations` | Escalate and view immutable escalation history |
| `PATCH /api/tickets/{id}/status` · `/assignment` | Change status / assignee |
| `GET /api/customers` · `/api/agents` · `/api/categories` | Reference data |

Errors are `ProblemDetails`: `400` validation, `404` not found, `409` conflict or business rule.

**Seed data:** 5 categories, 6 customers (3 Premium), 6 agents (one inactive, one at their limit)
and 40 tickets across every status, priority and SLA state.

## Troubleshooting

| Symptom | Fix |
| --- | --- |
| API cannot reach the database | `npm run db:up`, wait ~20 s, check `docker compose ps` |
| Port 1433 already in use | Change the port in `docker-compose.yml` and `.env` |
| Browser warns about the certificate | `dotnet dev-certs https --trust`, or use the web app, which proxies |
| Web app shows "Unable to load tickets" | The API is not running, or `VITE_API_PROXY_TARGET` points at the wrong port |
| Container API is unhealthy | Check `docker compose ps` and `docker compose logs api sqlserver`; confirm `.env` has the SQL password and a JWT key at least 32 bytes long |
| SQL Server slow on Apple Silicon | Enable Rosetta in Docker Desktop → Settings → General |
