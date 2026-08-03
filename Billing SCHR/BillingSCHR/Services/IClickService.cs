using ODULink.DTOs;

namespace ODULink.Services
{
    public interface IClickService
    {
        Task<ClickPrepareResponse> PrepareAsync(ClickPrepareRequest request);
        Task<ClickCompleteResponse> CompleteAsync(ClickCompleteRequest request);
        bool VerifySignature(ClickPrepareRequest request);
        bool VerifySignature(ClickCompleteRequest request);
    }
}
