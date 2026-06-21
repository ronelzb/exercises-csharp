# EF Core Internals

## Objective

Demonstrate three EF Core behaviors with real generated SQL, but observed output:
tracking vs. no-tracking, lazy loading and the N+1 problem, and `Include()`/`ThenInclude()` chains with `AsSplitQuery()`.

## Configuration

- [.NET 10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- Database file: `data/orders.db` (auto-created on first run, seeded with 500 orders)

## How to run

```bash
dotnet run --project src/EFCoreInternals
```

The database is created and seeded automatically on the first run. To reset and reseed, delete `data/orders.db`.

## How to test

Run the project and observe the console output. SQL statements are logged via `LogTo` with `EnableSensitiveDataLogging` — each exercise prints its own labeled output to separate observations.

## Exercises

### Tracking vs. AsNoTracking

Both tracked and no-tracking queries execute and appear in the SQL log. The difference is that `AsNoTracking()` results are never added to the change tracker — `db.ChangeTracker.Entries().Count()` stays at the same value it was before the no-tracking query ran.

A tracked entity is aware of any property change without an explicit `Update()` call. `EntityEntry.State` transitions to `Modified` as soon as a property is mutated, and `SaveChanges()` generates and executes an `UPDATE` statement automatically.

EF Core always uses parameterized queries (`@p0`, `@p1`, ...) regardless of logging settings — this is SQL injection prevention, not a logging feature. `EnableSensitiveDataLogging` is a separate opt-in that reveals the actual parameter values in the log output; it should only be enabled in development.

### Lazy loading and the N+1 problem

Without `Include()`, querying 50 orders and accessing `order.OrderLines` inside a loop fires one query per order on top of the initial fetch, 51 queries total for 50 orders. This is the N+1 problem: 1 query for the parent rows, N queries for the children.

Adding `.Include(o => o.OrderLines)` collapses this into a single query using a `LEFT JOIN`. EF Core loads all `OrderLine` rows alongside their parent orders in one round-trip, so accessing `order.OrderLines` inside the loop hits memory instead of the database, 1 query total.

### `Include()` chains and `AsSplitQuery()`

Chaining multiple collection `Include()`s in a single query causes a cartesian explosion — EF Core generates a `LEFT JOIN` per collection, duplicating the parent `Order` columns for every `OrderLine × StatusHistory` combination. An order with 5 lines and 2 status histories produces 10 rows in the result set instead of 1.

Adding `.AsSplitQuery()` splits execution into one `SELECT` per collection, each with an `INNER JOIN` back to the parent as a filter. Row duplication disappears, but each collection costs an extra round-trip to the database.

`ToQueryString()` only reveals the main entity query regardless of split mode — the collection queries are generated at execution time and are only visible in the `LogTo` output when `ToListAsync()` runs.
