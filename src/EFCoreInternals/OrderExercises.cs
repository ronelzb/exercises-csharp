using EFCoreInternals.Domain.Models;
using EFCoreInternals.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EFCoreInternals;

public static class OrderExercises
{
  public static async Task TrackingVsNoTracking(OrderDbContext db)
  {
    List<Order> tracked = await db.Orders.ToListAsync();
    Console.WriteLine("Tracked count: " + db.ChangeTracker.Entries().Count()); // 500

    List<Order> nontracked = await db.Orders.AsNoTracking().ToListAsync();
    Console.WriteLine("After AsNoTracking count: " + db.ChangeTracker.Entries().Count()); // still 500, no-tracking doesn't add entries

    Order target = tracked[0];
    bool originalDeleted = target.Deleted;
    target.Deleted = !originalDeleted;

    // show EF detected the change before saving
    EntityEntry<Order> entry = db.Entry(target);
    Console.WriteLine($"State before save: {entry.State}"); // Modified
    Console.WriteLine($"Deleted: {originalDeleted} → {target.Deleted}");

    await db.SaveChangesAsync(); // SQL UPDATE runs here — visible in LogTo output
    Console.WriteLine($"State after save: {entry.State}"); // Unchanged


    // non-tracked entity mutation is silently ignored
    nontracked[0].Deleted = true;
    await db.SaveChangesAsync();
    Console.WriteLine("Non-tracked mutation: " + db.ChangeTracker.Entries().Count()); // 0, nothing tracked
  }

  public static async Task LazyLoadingAndN1(OrderDbContext db)
  {
    // N+1: 1 query for orders + 1 per order for OrderLines = 51 total
    int queryCount = 0;
    IEnumerable<Order> orders = await db.Orders.Take(50).ToListAsync();
    queryCount++; // 1
    foreach (Order order in orders)
    {
      Console.WriteLine(order.Id + ": " + order.OrderLines.Count); // +1 each
      queryCount++;
    }
    Console.WriteLine($"N+1 query count: {queryCount}"); // 51

    db.ChangeTracker.Clear();

    // With Include: 1 query with JOIN
    queryCount = 0;
    IEnumerable<Order> ordersWithLines = await db.Orders
        .Include(o => o.OrderLines)
        .Take(50)
        .ToListAsync();
    queryCount++; // 1
    foreach (Order order in ordersWithLines)
    {
      Console.WriteLine(order.Id + ": " + order.OrderLines.Count); // no extra query
    }
    Console.WriteLine($"Include() query count: {queryCount}"); // 1
  }

  public static async Task IncludeChainsAndSplitQuery(OrderDbContext db)
  {
    var query = db.Orders
        .Include(o => o.OrderLines)
        .Include(o => o.StatusHistories);

    Console.WriteLine("=== Single Query SQL ===");
    Console.WriteLine(query.ToQueryString()); // console will show query with 2 LEFT JOINs

    var orders = await query.ToListAsync(); // same query as console


    db.ChangeTracker.Clear();

    var splitQuery = db.Orders
        .Include(o => o.OrderLines)
        .Include(o => o.StatusHistories)
        .AsSplitQuery();

    Console.WriteLine("=== Split Query SQL ===");
    Console.WriteLine(splitQuery.ToQueryString()); // console will show query with 2 LEFT JOINs again

    var ordersSplitted = await splitQuery.ToListAsync(); // but this time each include is queried separately using INNER JOIN
  }
}
