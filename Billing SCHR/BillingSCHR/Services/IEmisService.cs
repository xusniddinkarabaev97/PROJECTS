using ODULink.DTOs;

namespace ODULink.Services
{
    public interface IEmisService
    {
        Task<EmisCheckResponse> CheckPatientAsync(EmisCheckRequest request);
        Task<EmisPayResponse> ConfirmPaymentAsync(EmisPayRequest request);
    }
}
