namespace ODULink.Services
{
    public interface IAuditService
    {
        Task LogAsync(string userLogin, string action, string details, string ipAddress);
    }
}
