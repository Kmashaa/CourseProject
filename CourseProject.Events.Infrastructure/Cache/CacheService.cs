using CourseProject.Events.Application.Interfaces;
using CourseProject.Events.Domain.Entities;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace CourseProject.Events.Infrastructure.Cache
{
    public class CacheService : ICacheService
    {
        const string key = "events:";

        private readonly IDatabase _db;
        private readonly ILogger<CacheService> _logger;

        public CacheService(IConnectionMultiplexer multiplexer, ILogger<CacheService> logger)
        {
            _db = multiplexer.GetDatabase();
            _logger = logger;
        }

        public async Task<Event?> GetById(Guid id)
        {
            try
            {
                var keyForRequest = key + id.ToString();

                RedisValue cached = await _db.StringGetAsync(keyForRequest);

                if (cached.HasValue)
                {
                    return JsonSerializer.Deserialize<Event>(cached.ToString());
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.ToString());
                return null;
            }
        }

        public async Task<Event?> SetById(Guid id, Event @event)
        {
            try
            {
                await _db.StringSetAsync($"{key}{id}", JsonSerializer.Serialize<Event>(@event), TimeSpan.FromMinutes(15));
                return @event;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.ToString());
                return null;
            }
        }

        public async Task DeleteById(Guid id)
        {
            try
            {
                await _db.KeyDeleteAsync($"{key}{id}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.ToString());
            }
        }
    }
}
