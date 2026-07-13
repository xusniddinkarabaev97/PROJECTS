using GzsBilling.Domain.Entities;

namespace GzsBilling.Sverka.Reconciliation;

public interface IKunlikSverkaMenejeri
{
    Task ExecuteProcessKunlikSverkaAsync(DateOnly sana);
}
