using DomainDrivenDesign.Domain.Aggregates;
using DomainDrivenDesign.Domain.ValueObjects;
using DomainDrivenDesign.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DomainDrivenDesign;

public static class DddExercises
{
  private static void AddOrderLine(Order order, string prodName, decimal prodPrice, short quantity)
  {
    ProductSnapshot product1 = new()
    {
      ProductId = Guid.CreateVersion7(),
      Name = prodName,
      Price = Money.Create(prodPrice),
    };
    order.AddLine(product1, quantity);
    Console.WriteLine($"new order line added, order Total: {order.Total}");
  }

  public static async Task AggregateAndRoot(OrderDbContext db)
  {
    Console.WriteLine("=== Aggregate Root ===");

    // 1. Factory method records Draft automatically
    Order newOrder = Order.Create();
    Console.WriteLine($"Order created — Status: {newOrder.Status}, History entries: {newOrder.StatusHistories.Count}");

    // 2. Total stays consistent as lines are added
    AddOrderLine(newOrder, "Product1", .5m, 2);
    AddOrderLine(newOrder, "Product2", 2.3m, 5);

    // 3. Remove a line — Total should drop
    Guid lineToRemove = newOrder.OrderLines.First().Id;
    Console.WriteLine($"Total before RemoveLine: {newOrder.Total}");
    newOrder.RemoveLine(lineToRemove);
    Console.WriteLine($"Total after RemoveLine:  {newOrder.Total}");

    // 4. Guard: cannot Ship without Confirming first
    try
    {
      newOrder.Ship();
    }
    catch (InvalidOperationException ex)
    {
      Console.WriteLine($"Ship() from Draft blocked: {ex.Message}");
    }

    // 5. Guard: cannot AddLine after Confirm
    newOrder.Confirm();
    try
    {
      AddOrderLine(newOrder, "Product3", 1.7m, 3);
    }
    catch (InvalidOperationException ex)
    {
      Console.WriteLine($"AddLine() after Confirm blocked: {ex.Message}");
    }

    // 6. Persist and reload — verify StatusHistories survived the round-trip
    db.Orders.Add(newOrder);
    await db.SaveChangesAsync();
    db.ChangeTracker.Clear();

    Order reloaded = await db.Orders
        .Include(o => o.OrderLines)
        .Include(o => o.StatusHistories)
        .FirstAsync(o => o.Id == newOrder.Id);

    Console.WriteLine($"Reloaded — Lines: {reloaded.OrderLines.Count}, Total: {reloaded.Total}");
    Console.WriteLine($"Status history: {string.Join(" → ", reloaded.StatusHistories.Select(h => h.Status))}");
  }

  public static async Task BoundedContextSnapshot(OrderDbContext db)
  {
    Order newOrder = Order.Create();

    Guid productId = Guid.CreateVersion7();
    ProductSnapshot snapshot_v1 = new()
    {
      ProductId = productId,
      Name = "Widget",
      Price = Money.Create(10m),
    };
    newOrder.AddLine(snapshot_v1, 3);

    db.Orders.Add(newOrder);
    db.SaveChanges();

    // Simulating a change of Widget Product from another Catalog bounded-context
    ProductSnapshot snapshot_v2 = new()
    {
      ProductId = productId,
      Name = "Widget",
      Price = Money.Create(15m),
    };

    Console.WriteLine($"Ordered at: {snapshot_v1.Price} (v1)");
    Console.WriteLine($"Catalog updated to: {snapshot_v2.Price} (v2)");

    db.ChangeTracker.Clear();

    Order reloaded = await db.Orders
        .Include(o => o.OrderLines)
        .FirstAsync(o => o.Id == newOrder.Id);

    Money reloadedPrice = reloaded.OrderLines.First().ProductSnapshot.Price;
    Console.WriteLine($"Reloaded order line price: {reloadedPrice} — snapshot is frozen, Catalog change had no effect");

    // VO equality: same values = same snapshot, regardless of reference
    ProductSnapshot snapshot_v1_copy = new() { ProductId = productId, Name = "Widget", Price = Money.Create(10m) };
    Console.WriteLine($"v1 == v1_copy (same values): {snapshot_v1 == snapshot_v1_copy}");
    Console.WriteLine($"v1 == v2 (price differs):    {snapshot_v1 == snapshot_v2}");
  }

  public static async Task AnemicVsRich(OrderDbContext db)
  {
    Console.WriteLine("=== Anemic vs Behavior-Rich ===");

    Order newOrder = Order.Create();

    // [Anemic would allow] order.Status = OrderStatus.Cancelled after Shipped — no guard
    // [Rich model enforces] Cancel() rejects invalid transitions
    newOrder.Confirm();
    newOrder.Ship();
    try
    {
      newOrder.Cancel();
    }
    catch (InvalidOperationException ex)
    {
      Console.WriteLine($"[Rich] Cancel() after Ship blocked: {ex.Message}");
    }

    // [Anemic would allow] order.OrderLines.Add(new OrderLine { ... }) — direct collection mutation
    // [Rich model enforces] OrderLines is IReadOnlyCollection — Add() doesn't exist on the interface
    Console.WriteLine($"[Rich] OrderLines type: {newOrder.OrderLines.GetType().Name} — no Add(), no setter");

    // [Anemic would require] caller to recompute and set Total after every line change
    // [Rich model enforces] Total is always consistent — AddLine/RemoveLine own it
    Order freshOrder = Order.Create();
    ProductSnapshot product = new() { ProductId = Guid.CreateVersion7(), Name = "Gadget", Price = Money.Create(4m) };
    Console.WriteLine($"[Rich] Total before AddLine: {freshOrder.Total}");
    freshOrder.AddLine(product, 5);
    Console.WriteLine($"[Rich] Total after AddLine(qty=5, price=$4): {freshOrder.Total} — no manual recalculation needed");

    db.Orders.Add(freshOrder);
    await db.SaveChangesAsync();
  }
}
