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
reason is trimmed and must be 5–500 characters; `escalatedBy` is required and capped at 100 until
Task 3 derives it from the signed-in agent.

Each successful escalation and its immutable history row are saved in the same unit of work.
`GET /api/tickets/{id}/escalations` returns the rows newest first with before/after priority, owner,
and due date. `TicketEscalations` cascades when its ticket is deleted, while agent references use
restrict behavior to preserve actor/owner references. The composite index
`IX_TicketEscalations_TicketId_EscalatedAtUtc` starts with `TicketId` for the history lookup and
supports newest-first ordering. `IX_TicketEscalations_FromAgentId` and
`IX_TicketEscalations_ToAgentId` support the restrictive agent foreign keys without table scans.

---

## The assignment

You are joining a team that already has this app working. Complete the tasks **in order**.
Full requirements are in **[`ASSIGNMENT_CANDIDATE.md`](ASSIGNMENT_CANDIDATE.md)**.

| # | Task | Done looks like | Effort |
| --- | --- | --- | --- |
| 1 | **Fix a defect, add server-side filtering** | Root cause fixed with a regression test; every list filter applied in the database with a correct count | ~2.5 h |
| 2 | **Ticket escalation & assignment** | Automatic triage on create, escalation with history, SLA status, UI for all of it | ~7 h |
| 3 | **Login and authentication** | Agents sign in; API rejects unauthenticated calls; login UI | ~4 h |
| 4 | **Dockerise the API** *(optional)* | `docker compose up` gives a migrated, seeded API with no baked-in secret | ~1.5 h |

### Guidance

- **Time: 2 days.** Honestly marking a task "not finished" beats rushing all of them.
- **Fork** this repo to your own GitHub account and work there. **Do not push to the original.**
- **AI tools are allowed.** You must understand and be able to defend every line you submit.
- **Commit as you go** and keep the real history. Commit the Task 1 defect fix and the filtering
  separately from each other and from the feature work. A single squashed commit is not accepted.
- **Where the spec is ambiguous**, make a decision, write it down in the README, and move on.
- **Out of scope:** registration, password reset, roles, OAuth/SSO, refresh tokens, MFA,
  notifications, CI, caching, visual redesign. Mention them under *Known limitations* instead.

### Submission

1. Update this **README** with what you built, your design decisions (chosen, rejected and why),
   the defect's root cause, dev login credentials, how to run and test it, AI usage, and known
   limitations. A reviewer must be able to run it without asking you anything.
2. Add an **APPROACH.md** explaining your approach and thought process for each task.
3. Commit both, then send us the **link to your fork**, the **dev login credentials**, and a
   **rough note of time spent** on what. (If the fork is private, add the reviewers as collaborators.)

---

## Quick start

**Prerequisites:** .NET SDK 10 (`global.json`), Node.js 22 (`.nvmrc`; 20.19+ works), Docker.

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
```

Both pass on a clean checkout and neither needs a database.

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
| `GET /api/tickets` | Paged list with database-side `search`, `status`, `priority`, `categoryId`, `customerId`, `assignedAgentId`, `unassignedOnly`, `sortBy`, `sortDirection`. |
| `GET /api/tickets/{id}` | One ticket |
| `POST /api/tickets` | Create a ticket |
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
| SQL Server slow on Apple Silicon | Enable Rosetta in Docker Desktop → Settings → General |
