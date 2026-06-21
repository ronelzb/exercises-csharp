namespace EFCoreInternals.Domain.Models;

public abstract class EntityBase
{
  public Guid Id { get; init; } = Guid.CreateVersion7();

  public bool Deleted { get; set; }
  public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
  public DateTimeOffset LastModifiedAt { get; set; }
}
