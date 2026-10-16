# Approach

This note records the implementation choices and verification for each assignment task. The README
contains run instructions and detailed behavior. The candidate's screenshots and sanitized login
response are supporting evidence.

## Task 1 — Defect and database filtering

The ticket hook's fetch effect watched search and pagination state but omitted the other filters.
Changing status updated React state without requesting the API; a later search change sent the
accumulated state. The fix makes non-search filter changes request immediately and keeps the debounce
limited to search. A regression test checks the actual request arguments and timing.

The API composes status, priority, category, customer, agent, unassigned, SLA, and search predicates
on the EF Core query before count, ordering, and paging. Search trims its input and checks title,
reference, and customer name. This keeps count and pages aligned with the filtered SQL result and
allows another filter to be added as another predicate.

## Task 2 — Triage, SLA, and escalation

Category flags provide forced-critical and specialist behavior without hard-coded category names.
Configuration supplies base SLA windows, the premium multiplier, the minimum window, and the at-risk
threshold. One triage service applies priority, SLA, and assignment decisions on both create and
escalate. Eligible agents are active, qualified where required, and below capacity; the least-open
agent wins, with agent ID as the deterministic tie-break. No eligible agent is a successful
unassigned outcome.

SLA status is derived from due and resolution times using the injected clock; it is not persisted.
Escalation raises priority one level, recalculates the deadline from escalation time, rechecks the
current assignee, and saves an immutable history row with the ticket update. Critical, resolved, and
closed tickets are rejected. The UI shares one labelled SLA indicator and refreshes ticket/history
data after escalation.

## Task 3 — Authentication

Credentials are stored on `Agent`, the existing identity aggregate, rather than introducing a second
user table. ASP.NET Identity's `PasswordHasher<Agent>` stores a salted one-way hash. Login issues a
60-minute JWT; a fallback authorization policy protects the API, while login and health are
anonymous. The escalation actor comes from the token. The frontend keeps the token in localStorage
for reload persistence and clears it on logout or an unauthorized response; the README records the
same-origin script-injection trade-off.

## Task 4 — Docker

The API Dockerfile restores from copied project and shared build files before copying source, then
publishes with the .NET 10 SDK and runs from the ASP.NET runtime image as a non-root user on HTTP
8080. Compose supplies the SQL connection, JWT settings, and environment at runtime, waits for the
SQL health check, and enables migration and development seeding through a runtime flag. The database
health endpoint is registered centrally and marked anonymous. A real deployment should run schema
changes as a controlled job rather than grant every API replica migration rights.

## Design, verification, and evidence

The existing domain/application/infrastructure/presentation layers were retained. Small handlers
keep operations readable, while `TicketTriageService` and `IClock` represent shared rule and time
seams. A mediator, generic repository abstraction, persisted SLA status, refresh-token flow, and
production migration runner were not added because they add complexity outside the assignment's
needs.

The candidate ran backend unit and architecture tests, frontend Vitest tests and typechecking, and
manual login and Docker checks. On an empty Compose volume, startup migrated and seeded 40 tickets;
`/health` was healthy, restore stayed cached after a source-only change, `whoami` returned `app`, and
the runtime and SDK image sizes were recorded in the README. `screenshots/` contains the candidate's
test evidence. `login.json` is included to show the development login response shape; its temporary
JWT was replaced with `[REDACTED: development JWT omitted]` before submission, so it cannot
authenticate. Both evidence artifacts are excluded from the Docker build context.

The final build exposed a vulnerable transitive `System.Security.Cryptography.Pkcs` 9.0.11 dependency
from `Microsoft.Data.SqlClient`. The central package version pins the dependency to patched 9.0.21;
NuGet restore completed without vulnerability warnings after the update. On Node 22.22.2 the
candidate reran the checks: all 53 backend and 17 frontend tests passed, and build, lint, and
typecheck completed successfully.

Codex helped inspect the code, implement and explain changes, draft tests, and interpret failures.
One AI-provided PowerShell setup line was run in Command Prompt and failed; it was replaced with a
CMD command. Docker Compose also suggested a different position for its progress flag; the corrected
form was used. Candidate-run checks, rather than unverified AI claims, are the verification evidence.

The optional SLA summary report and the out-of-scope authentication and deployment features listed
under Known limitations in the README remain unimplemented. Actual time spent was not tracked and
should be supplied by the candidate in the submission note.
