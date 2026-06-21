using EFCoreInternals.Domain.Enums;
using EFCoreInternals.Domain.Models;
using EFCoreInternals.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EFCoreInternals;

public static class OrderDbSeeder
{

  public static async Task<bool> IsSeeded(OrderDbContext db)
  {
    await db.Database.EnsureCreatedAsync();
    return await db.Orders.AnyAsync();
  }

  private static decimal NextDecimal(Random random, decimal minValue, decimal maxValue)
  {
    // Generate a random double, cast to decimal, and scale to the range
    decimal scale = (decimal)random.NextDouble();
    return minValue + (scale * (maxValue - minValue));
  }

  public static async Task SeedAsync(OrderDbContext db)
  {
    Console.WriteLine("No previous seeding detected, creating seed");

    Random random = new(42);
    IEnumerable<Product> products = [.. Enumerable.Range(1, 15)
      .Select(i => new Product
                    {
                      Name = $"Product {i}",
                      Price = random.Next(5, 200),
                    }
      )];

    await db.AddRangeAsync(products);

    OrderStatus[] statuses = Enum.GetValues<OrderStatus>();

    for (int i = 0; i < 500; i++)
    {
      Order order = new();

      // Attach 1-5 random products to the current order
      int lineCount = random.Next(1, 6);
      for (int lcIdx = 0; lcIdx < lineCount; lcIdx++)
      {
        Product product = products.ElementAt(random.Next(products.Count()));

        // add 1-5 OrderLines from products pool
        for (int j = 0; j < random.Next(1, 6); j++)
        {
          OrderLine orderLine = new()
          {
            Order = order,
            Product = product,
            Quantity = random.Next(1, 10),
            UnitPrice = NextDecimal(random, .5m, 10m),
          };

          db.OrderLines.Add(orderLine);
        }
      }

      // add 1–2 StatusHistories
      for (int j = 0; j < random.Next(1, 3); j++)
      {
        StatusHistory statusHistory = new()
        {
          Order = order,
          Status = statuses[random.Next(statuses.Length)],
        };

        db.StatusHistories.Add(statusHistory);
      }

      db.Orders.Add(order);
    }

    await db.SaveChangesAsync();
  }
}
