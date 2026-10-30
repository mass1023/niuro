# Architecture

## Structure

```
backend/src/
  Domain/          Customer, LoanApplication, Ssn, Address, LoanRequest, IDenialPolicy. No dependencies.
  Application/     the submit use case, the decision engine, the policies, ports (repositories, outbox, unit of work, external client)
  Infrastructure/  EF Core + Postgres, the outbox and its background processor, the HTTP client for the mock
  Api/             the controller, request validation, error mapping, DI wiring
mock-service/      standalone minimal API that plays the external customer service
frontend/          Next.js, one page with the form and the result
```

Dependencies point inward: Api → Application → Domain, and Infrastructure implements the Application ports. Application doesn't know about EF or HTTP.

## Request flow

1. The controller validates the request shape and hands it to `SubmitLoanApplicationHandler`.
2. The handler builds the domain objects (`Ssn` and `Address` normalize the input) and runs every denial policy.
3. If any policy is violated, the request is denied and nothing is saved.
4. If not, the handler does the following, all through **one** `SaveChanges`:
   - creates or updates the customer, looked up by SSN
   - creates or updates their application
   - writes an outbox message

A denial isn't an error: the API returns 200 with `approved: false` and the reasons.

## Adding a rule

Write a class that implements `IDenialPolicy`:

```csharp
public sealed class MaxAmountPolicy : IDenialPolicy
{
    public string Reason => "The requested amount is too high.";
    public bool IsViolatedBy(LoanRequest request) => request.RequestedAmount > 1_000_000m;
}
```

Then register it in `Application/DependencyInjection.cs`:

```csharp
services.AddSingleton<IDenialPolicy, MaxAmountPolicy>();
```

Nothing else changes. The engine runs every registered policy and collects all the reasons, so the user sees everything that's wrong at once.

## Saving and publishing as one unit

Saving the customer, saving the application and publishing the event have to succeed or fail together. Calling the mock inside the database transaction doesn't work:

- The HTTP call can succeed and the commit can still fail afterwards (lost connection, crash), and an HTTP call can't be rolled back.
- The user would wait on the external service, and the spec wants the event outside the request.
- If the mock is down, nobody could get approved.

So I used a transactional outbox: "publishing" means inserting a row in `OutboxMessages` in the same transaction as the customer and the application. If anything fails, Postgres rolls back all three rows. `SubmitLoanApplicationTests.WhenOutboxInsertFails_CustomerAndApplicationAreRolledBack` forces the outbox insert to fail with a trigger and checks that all three tables are empty.

## Background event

`OutboxProcessor` is a `BackgroundService` inside the API. Every 2 seconds it:

1. takes up to 20 pending messages, oldest first
2. sends each one to the mock
3. marks it as processed

If a send fails, it records the error and the attempt count, then stops the batch. The message is retried on the next tick. Stopping keeps messages in order, so an older snapshot can never overwrite a newer one.

The contract with the mock is a single endpoint:

```
PUT /customers/{customerId}
{ "customerId", "firstName", "lastName", "street", "city", "state", "zipCode",
  "companyName", "ssn", "loanApplicationId", "requestedAmount" }
→ 200
```

It's an upsert keyed by our customer id: the first call creates the record and later calls replace it. A returning customer keeps the same id, so they update the existing record instead of creating a new one.

Delivery is at-least-once. If the processor crashes after the mock answered but before the message was marked, the message is sent again. That's harmless because the PUT just writes the same snapshot again.

The event carries a full snapshot of the data, not just ids. The processor sends it as it is, without reading the tables again.

## Returning customer

There are unique indexes on `Customers.Ssn` and `LoanApplications.CustomerId`, so one SSN always maps to one customer and one application, enforced by the database.

The SSN is normalized to 9 digits first, so `123-45-6789` and `123456789` count as the same person. A new application from the same SSN **replaces** the old data and amount; it doesn't add to them.

## Other decisions

- **Controller in the API, minimal API in the mock.** The API follows the usual production shape. The mock is a throwaway stand-in, so it stays as small as possible.
- **Ids are generated in the domain** (`Guid.CreateVersion7()`), so the outbox payload has them before the save.
- **`Address` is an EF complex property**: its fields are columns on `Customers`, with no extra table.
- **The blacklist reason is vague** ("not eligible"), so we don't tell someone they're on a list.
- **Migrations run on startup in Development only**, so running locally is just `docker compose up` and `dotnet run`.
- **`Ssn.ToString()` is masked**, so the SSN doesn't leak through logs.

## Trade-offs

- **Polling every 2 seconds instead of a message broker.** It adds a small delay and some idle queries, but needs no extra infrastructure. Swapping in a broker would only change Infrastructure.
- **A single processor instance.** Running several API instances would need row locking (`FOR UPDATE SKIP LOCKED`) and per-customer ordering.
- **No retry limit or dead-letter queue.** A message that keeps failing blocks the ones behind it until the mock recovers. That's acceptable for a mock, but not for production.
- **Validation lives in two places.** The API validates the request shape and the domain guards its own invariants. A little duplication, in exchange for field-level errors in the UI and a domain that can't be put in a bad state.
