using GzsBilling.Domain.Models;

namespace GzsBilling.Infrastructure.Clients;

public interface IUGazInfrastrukturaClient
{
    Task<UGazSeansResponse?> GetZapravkaSeansAsync(int fillingStationId, int dispenserId);
}
