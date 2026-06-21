using EFCoreInternals.Domain.Enums;

namespace EFCoreInternals.Domain.Models;

public class StatusHistory : EntityBase
{
  public Guid OrderId { get; set; }
  public virtual required Order Order { get; set; }

  public required OrderStatus Status { get; set; }
}
