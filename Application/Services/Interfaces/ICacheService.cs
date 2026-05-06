namespace Application.Services.Interfaces
{
    public interface ICacheService
    {
        Task<string> GetAsync(string key);
        Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
        Task SetRawAsync(string key, string value, TimeSpan? expiry = null);
        Task RemoveAsync(string key);
    }
}
