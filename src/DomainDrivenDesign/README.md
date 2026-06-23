# Domain Driven Design

## Objective

The goal is to map real architecture to the vocabulary, so the next interview answer is concrete and experience-backed rather than textbook.

## Configuration

- [.NET 10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)

## How to run

```bash
dotnet run --project src/DomainDrivenDesign
```

## Exercises

### Aggregates and Aggregate Root

**Domain:** `Order` → `OrderLine` → `ProductSnapshot`

An **Aggregate** is a cluster of domain objects treated as a single unit of consistency. The **Aggregate Root** (`Order`) is the only entry point — no external code touches `OrderLine` or `StatusHistory` directly.

#### Key decisions and why

**Private backing field + `IReadOnlyCollection`**
`_orderLines` is a `List<OrderLine>` exposed as `IReadOnlyCollection<OrderLine>`. Callers can read lines but cannot add, remove, or replace them without going through `Order`'s methods. `ICollection` with a public setter would break aggregate encapsulation.

**`Order.Create()` factory method + private constructor**
The constructor is private so no caller can bypass the factory. `Create()` is the only sanctioned way to instantiate an `Order`, guaranteeing the initial `Draft` status is always recorded in `StatusHistory`. With a public constructor any caller could skip that step. EF Core materializes existing orders using the private constructor via reflection — no Draft entry is added on load because the DB rows already carry it.

**`ProductSnapshot` as a Value Object, not an Entity reference**
`OrderLine` does not hold a navigation to a live `Product`. It captures a `ProductSnapshot` (name + price + original product ID) at order time. If the product price changes later, the order total is unaffected. This also respects the bounded context boundary — the Catalog context owns `Product`; the Order context only keeps what it needed at the moment of purchase.

**Value Object equality via `GetEqualityComponents()`**
`ValueObject` base class implements `Equals` / `GetHashCode` / `==` / `!=` through an abstract `GetEqualityComponents()`. Two `ProductSnapshot`s with the same `ProductId`, `Name`, and `Price` are equal regardless of reference. VOs use `required init` properties — immutable after construction.

**`Money` as a Value Object instead of `decimal`**
Arithmetic operators are defined as `Money * int` and `Money * decimal` — not `Money * Money`, because multiplying two monetary amounts has no real business meaning. Currency mismatches throw at runtime rather than silently producing a wrong result.

**Status transitions as guarded methods**
`Confirm()`, `Ship()`, and `Cancel()` each validate `Status` before transitioning and throw `InvalidOperationException` on invalid calls. The enforced flow is `Draft → Confirmed → Shipped`, with `Cancelled` reachable from `Draft` or `Confirmed` only.

**`StatusHistory` auto-recorded in the `Status` setter**
Using C# 13's `field` keyword, the `Status` property setter appends a `StatusHistory` entry on every transition, recording the *to-state* (not the from-state) so history reads as a timeline of what the order became. `Draft` is recorded by `Order.Create()` because property initializers bypass setters.

**`LastModifiedAt` stamped at the infrastructure layer**
`EntityBase.LastModifiedAt` has `internal set`. The domain model never calls a `MarkModified()` method. Instead, `OrderDbContext.SaveChanges()` stamps it on every `EntityState.Modified` entry via the change tracker. Audit concerns stay out of the domain model entirely.

**Only `Order` is a `DbSet<>`**
`OrderLine` and `StatusHistory` are not registered as `DbSet<>` entries. They are queried exclusively through `context.Orders.Include(o => o.OrderLines)`. Exposing them independently would let callers bypass the aggregate root.

**`OnModelCreating` without reverse navigation**
`OrderLine` has no navigation property back to `Order` — only `OrderId` (FK). EF is configured with `.WithOne()` (no nav) and `.HasForeignKey(ol => ol.OrderId)`. Children do not reference their root; the root holds its children.

**Value Objects persisted with `OwnsOne`, not as separate tables**
EF Core requires a primary key for every entity it tracks. `Money` and `ProductSnapshot` have none — they are Value Objects. Configuring them with `.OwnsOne()` tells EF to store their properties as columns on the owning table rather than creating a separate table. Nested VOs (`ProductSnapshot.Price` is `Money`) use nested `.OwnsOne()` calls. Computed properties like `OrderLine.LineTotal` must be explicitly ignored with `.Ignore()` or EF will try to map the return type as an entity.

### Identify bounded contexts

A **Bounded Context** is a named boundary within which a specific domain model applies.

#### The signal: a navigation property you don't own

In `EFCoreInternals`, `OrderLine` had a `virtual required Product Product` navigation. That means the Order context held a live reference to an entity owned by the Catalog context. Any price change in Catalog would silently affect existing orders, a classic cross-context coupling bug.

The question that reveals the boundary: **"Who owns this data, and who needs a snapshot of it?"**

| Context | Owns | Needs from Order context |
| --- | --- | --- |
| **Order** | `Order`, `OrderLine`, `StatusHistory` | a frozen copy of product name + price at purchase time |
| **Catalog** | `Product`, pricing rules, stock | order IDs (for reporting), nothing structural |

#### How this maps to code

`OrderLine` does not reference `Product`. It holds a `ProductSnapshot`, an immutable Value Object capturing `ProductId`, `Name`, and `Price` at the moment the line was added. After that point, the two contexts are decoupled:

- Catalog can change prices freely, existing orders are unaffected.
- Order can be deployed independently of Catalog.

#### The rule: reference by ID across context boundaries

If the Order context ever needs to look up a live product (e.g., to show current stock), it calls the Catalog API or reads a Catalog `DbContext`, it does not navigate through a foreign key into Catalog's tables from Order's own aggregate. The boundary is enforced architecturally, not just by convention.

#### How to spot a boundary violation

- A navigation property to an entity you don't write to (you only read it).
- A change in another service/module can break your aggregate's invariants.
- Your `DbContext` has `DbSet<>` entries for tables owned by a different team or module.

### Anemic vs. behavior-rich domain model

An **anemic model** is one where domain classes are little more than data bags, public getters and setters everywhere, with all business logic living in services or controllers. It looks object-oriented but behaves like a procedural data structure.

A **behavior-rich model** pushes logic into the entity itself, so the object enforces its own invariants and callers cannot put it into an invalid state.

#### Side-by-side: `EFCoreInternals` (anemic) vs `DomainDrivenDesign` (rich)

##### Setting a status

```csharp
// Anemic — caller decides everything, nothing is enforced
order.Status = OrderStatus.Shipped;

// Behavior-rich — the object guards the transition
order.Ship(); // throws if not Confirmed first
```

##### Adding a line

```csharp
// Anemic — caller mutates the collection directly
order.OrderLines.Add(new OrderLine { ProductId = id, Quantity = 3, UnitPrice = 9.99m });

// Behavior-rich — the root controls entry, enforces Draft guard, keeps Total consistent
order.AddLine(snapshot, quantity: 3);
```

##### Soft delete

```csharp
// Anemic — any caller flips the flag
order.Deleted = true;

// Behavior-rich — intent is explicit, extensible (events, rules)
order.MarkAsDeleted();
```

#### Where the anemia shows up in code

| Smell | What it means |
| --- | --- |
| Public setters on domain properties | Caller can put the object in any state, valid or not |
| `Status` set from a service class | Transition rules live outside the entity — easy to skip |
| `OrderLines` as `ICollection` with public `set` | Collection can be replaced or mutated without going through the root |
| `UnitPrice` passed by the caller | The price source of truth is not the snapshot — caller decides |
| `Total` recalculated in a service | The aggregate doesn't own its own invariant |

#### The test: can a caller violate an invariant without throwing?

If the answer is yes, ship before confirm, add a line to a shipped order, set a negative quantity, the model is anemic in that area. The behavior-rich version makes those paths impossible without an exception.
