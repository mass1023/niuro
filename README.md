# Loan application

Video: https://youtu.be/3qcZce-bhPQ

A small loan application flow: a Next.js form, a .NET 10 API that approves or denies the request, Postgres for storage, and a mock external service that receives approved customers in the background.

Design notes and trade-offs are in [ARCHITECTURE.md](ARCHITECTURE.md).

## What you need

- .NET 10 SDK
- Node.js 20+
- Docker (for Postgres and for the integration tests)

## Run it

Use four terminals, all starting from the repo root.

```bash
# 1. Postgres (port 5440)
docker compose up -d

# 2. Mock customer service (port 4000)
dotnet run --project mock-service/MockCustomerService

# 3. API (port 5088). Applies the migrations on startup.
dotnet run --project backend/src/Api

# 4. Frontend (port 3000)
cd frontend
npm install
npm run dev
```

Open http://localhost:3000.

## Tests

```bash
cd backend
dotnet test
```

The unit tests cover the rules. The integration tests start a throwaway Postgres with Testcontainers (Docker has to be running) and cover:

- the returning customer
- the rollback when the save fails
- the outbox processor
- the endpoint

## Test data

| Case | Input | Result |
|---|---|---|
| Approved | any state except New York, SSN `444-55-6666` | Approved. The mock logs `-> created` |
| Returning customer | submit `444-55-6666` again with a different name, company or amount | Approved. Same customer and application are updated; the mock logs `-> updated` with the same id |
| New York | state New York | Denied |
| Blacklisted SSN | `123-45-6789` or `111-22-3333` | Denied |

The blacklist is in `backend/src/Api/appsettings.json`.

`backend/src/Api/Loans.Api.http` has the same requests if you want to call the API directly.

## Not done / known limits

- No authentication.
- SSNs are stored as plain text, and the full SSN is sent to the mock, which logs the whole payload.
- The mock keeps its data in memory, so it forgets everything when restarted.
- If two first-time applications with the same SSN arrive at exactly the same moment, one gets a 409 and has to be resubmitted.
- The outbox processor assumes a single API instance.
