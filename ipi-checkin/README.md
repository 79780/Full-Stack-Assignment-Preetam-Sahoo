# Specimen Check-In — IPI Pro

A vertical slice of the specimen receiving desk: a technician opens a manifest, marks each
bottle received, flags the ones that never arrived, and closes the manifest once every bottle
is accounted for. Two pathology labs share the deployment and neither can see the other's work.

- **Frontend** — Vue 3 + TypeScript + Vite
- **Backend** — ASP.NET Core 8 Web API, EF Core
- **Database** — SQLite (chosen so `dotnet run` is the entire setup; see [Database choice](#database-choice))

---

## Running it

Two terminals. The API first.

```bash
cd backend/src/IpiPro.Api
dotnet run
```

It creates `ipipro.db`, applies the migration, seeds two labs, and listens on
`http://localhost:5080`. Swagger UI is at `/swagger`.

```bash
cd frontend
cp .env.example .env
npm install
npm run dev
```

Open `http://localhost:5173`. Use the lab selector in the top right to switch between
**Northgate Pathology** and **Ridgeview Diagnostics** — the worklist changes completely,
because the second lab's manifests were never sent to the browser.

### Tests

```bash
cd backend
dotnet test
```

Fourteen tests: eight on the reconciliation rules (idempotency, discrepancy lifecycle, close
gating) and six on the tenant boundary itself.

---

## How the pieces fit

```
X-Lab-Id header
      │
      ▼
TenantMiddleware ──► TenantContext (scoped, write-once)
                            │
                            ▼
                     AppDbContext
                     ├── global query filters   (reads)
                     └── SaveChanges guard      (writes)
                            │
                            ▼
                      CheckInService  ──► rules, no LabId anywhere
                            │
                            ▼
                      ManifestsController
```

`X-Lab-Id` stands in for authentication. In production the lab id comes from a validated token
claim; `TenantMiddleware` is the only file that would change, because nothing else reads the
request — everything reads `ITenantContext`.

### API

| Method | Route | Notes |
| --- | --- | --- |
| `GET` | `/api/labs/current` | Which lab this request is scoped to |
| `GET` | `/api/manifests` | Worklist, open manifests first |
| `GET` | `/api/manifests/{id}` | Detail. 404 if it belongs to another lab |
| `POST` | `/api/manifests/{id}/specimens/{specimenId}/receive` | Idempotent |
| `POST` | `/api/manifests/{id}/specimens/{specimenId}/flag` | Raises one open discrepancy |
| `POST` | `/api/manifests/{id}/close` | Rejected unless reconciled |

Every mutation returns the whole manifest, counts included. The client never derives
"ready to close" for itself, so the button and the endpoint cannot disagree.

Failures come back as RFC 7807 `problem+json` with a stable `code`:
`manifest_not_reconciled`, `manifest_closed`, `manifest_already_closed`,
`specimen_already_received`, `not_found`, `missing_tenant`, `unknown_tenant`.

### Rules worth stating

- **Reconciled** means no bottle is still `Pending` — each one has been received or flagged.
- **Receive is idempotent.** Scanning a bottle twice, or retrying a request that timed out,
  is a success that moves no counter. Receiving a bottle that was flagged as missing resolves
  its discrepancy: it turned up.
- **Flag is idempotent too.** It raises exactly one open discrepancy however often it is called.
  Flagging an already-received bottle is refused (`specimen_already_received`) — un-receiving is
  a supervisor action and is out of scope here.
- **Closing** is refused while anything is pending. A manifest that reconciles with bottles still
  missing closes into `ClosedWithDiscrepancy`: the shipment leaves the technician's desk while the
  discrepancies stay open for whoever chases the clinic.

### Database choice

SQLite. The schema uses nothing exotic, EF Core abstracts the provider, and it means a reviewer
runs `dotnet run` and gets a working, seeded database with no container and no connection string
to edit. Moving to Azure SQL is a provider swap and a connection string; the one thing I would
re-check is `DeleteBehavior.Restrict` on `Discrepancy → Specimen`, which is already set precisely
because SQL Server rejects multiple cascade paths.

---

## Write-up

### 1. Deploying this on Azure

**App Service (Linux) for the API, Static Web Apps for the Vue bundle, Azure SQL for the data.**

A request lands on Front Door, terminates TLS, and reaches an App Service instance behind a VNet
integration. The API talks to Azure SQL over a private endpoint — the database has no public
listener. Secrets are not in `appsettings.json`: the App Service uses a managed identity to read
its connection string from Key Vault, and to authenticate to SQL, so there is no password to
rotate or leak. The Vue app is static and cached at the edge; it holds no secrets, because the
lab identity is proved by a token, not by anything the bundle knows.

The three pieces I would add before this is real:

- **Auth.** Entra ID (or an OIDC provider) issues a token carrying a `lab_id` claim.
  `TenantMiddleware` reads the claim instead of the header, and everything below it is unchanged.
- **Background work.** Chasing an open discrepancy is not a request-response job. A Service Bus
  queue and an Azure Function make the "notify the clinic about missing bottles" path retryable
  and independent of the technician's browser.
- **Observability.** Application Insights with the lab id on every trace, so a support engineer
  can answer "what did Northgate see at 09:14" without querying patient rows.

Scale is not the interesting problem here. A receiving desk is a handful of technicians per lab,
so a single App Service plan with two instances covers redundancy rather than throughput; Azure
SQL sizing is driven by manifest history, not by concurrency. I would keep the API stateless and
let session live entirely in the token.

### 2. Tenant isolation, and keeping it once the codebase grows

Isolation is enforced in exactly two places, both inside `AppDbContext`.

**Reads.** Every tenant-owned entity carries its own `LabId` (`ITenantOwned`) and gets a global
query filter, `x.LabId == CurrentLabId`. `CurrentLabId` reads from the request-scoped
`ITenantContext`. A developer writing a new query cannot forget the filter, because they never
write it. Rows from another lab do not appear in a `Where`, in an `Include`, or in a `Count`.

**Writes.** A query filter does not protect an `Update` on a hand-built entity with an id in it.
So `SaveChanges` inspects the change tracker: inserts missing a `LabId` are stamped with the
current lab, and anything — inserted, modified, deleted — whose `LabId` disagrees with the request
throws `CrossTenantWriteException`. That exception is logged at error level and returned to the
caller as a plain `404`, because a `403` would confirm the row exists.

For the same reason, another lab's manifest is `not_found`, never `forbidden`. The query filter
means it genuinely does not exist as far as this request is concerned, and the API says so.

**Testing it as the codebase grows.** `TenantIsolationTests` pins the boundary rather than the
endpoints: a lab sees only its own manifests, another lab's manifest is 404, a cross-lab receive
mutates nothing, a mis-stamped insert is refused, a forgotten `LabId` is stamped. These keep
holding as endpoints are added, because they test the mechanism. The tests run against a real
SQLite database rather than the EF in-memory provider, so unique indexes and foreign-key behaviour
are the ones we ship. `TestHarness` calls `Migrate()`, not `EnsureCreated()`, so a migration that
drifts from the model fails the suite.

What I would add next, in order: an integration test that fires a real HTTP request with a forged
`X-Lab-Id` once auth exists; an analyzer or architecture test asserting no `DbSet` is queried with
`IgnoreQueryFilters()` outside `Data/` and the test project; and a nightly job that scans for rows
whose `LabId` disagrees with their parent manifest's.

### 3. Handling PHI

The rule I designed to is that patient data is only ever in two places: the database, and the
response to a request that has already proved which lab it belongs to.

- **In transit.** TLS everywhere, no exceptions, HSTS on the front door. CORS is an allow-list of
  known origins, not a wildcard.
- **At rest.** Azure SQL with Transparent Data Encryption, and Always Encrypted on `Patient` if a
  DPO asks for it — the column is never queried, only projected, so the performance cost is small.
- **In logs.** Nothing in this codebase logs a patient name. Errors log the specimen *code* and the
  manifest *code*, which are meaningless without the database. The `CrossTenantWriteException`
  message names two lab ids and no rows. That is deliberate: logs leave the trust boundary — they
  go to Application Insights, to a support engineer's screen, into a screenshot in a ticket.
- **Access.** Least privilege on the SQL identity (no `db_owner` for the app), no shared accounts,
  and an audit trail. This slice records `ReceivedAt` and `ClosedAt` but not *who* did it; the first
  thing I would add under a HIPAA audit requirement is an actor column on every state transition,
  because "the manifest was closed" is not a useful audit record without "by whom".
- **In seed data.** The seeded patients are invented. A repository that ships a realistic-looking
  patient list teaches the next engineer that it is fine to paste one in.

The honest limitation: `X-Lab-Id` is a header a client sets. It is a placeholder for a token claim,
and until it becomes one, this application authorises nothing — it only *scopes*. The isolation
machinery is real and tested; the identity it scopes to is not yet proved.

---

## With more time

- Off-manifest bottles. `DiscrepancyType.OffManifest` and the nullable `SpecimenId` are modelled
  for it, but there is no endpoint: a bottle that is not on the list needs a scan-anything input,
  which is a different interaction than the row-by-row table here.
- A real scanner path. `Receive` takes a specimen id; a barcode gun gives you a code, so
  `POST /manifests/{id}/scan { code }` resolving to a specimen — or to an off-manifest discrepancy.
- Optimistic concurrency. Two technicians on one manifest currently last-write-wins. A `rowversion`
  on `Specimen` plus a `409` would be a small change and a real one.
- An actor on every transition, per the note above.
- Component tests for the Vue layer. The composable is the thing worth testing — the components
  are close to presentational.

## Notes on the build

The backend was written without a compiler to hand, so if `dotnet restore` turns up a package
version I got wrong, that is why. `dotnet build` and `dotnet test` are the first things I would
have you run. The frontend was built and type-checked (`npm run build` runs `vue-tsc --noEmit`).

The design reference was matched by eye — worklist left, detail right, status pills, running
counts, close gated on reconciliation. The one place I spent a decision: specimen and manifest
codes are set in tabular monospace, because a technician reads them character by character against
a label on a bottle.
