using System.Threading.Tasks;

namespace lib.cache.bifrost.interceptor
{
    public interface ICacher
    {
        Task<T> Run<T>(string id, CacheableCall<T> call, ValidValue<T> isValid);
    }
}