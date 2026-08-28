using CourseProject.Events.Application.Interfaces;
using CourseProject.Events.Domain.Entities;
using Microsoft.Extensions.Configuration;
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
        private readonly IConfiguration _configuration;


        public CacheService(IConnectionMultiplexer multiplexer, ILogger<CacheService> logger, IConfiguration configuration)
        {
            _db = multiplexer.GetDatabase();
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<Event?> GetById(Guid id)
        {
            try
            {
                var keyForRequest = CacheKeys.EventById(id);

                RedisValue cached = await _db.StringGetAsync(keyForRequest);

                if (cached.HasValue)
                {
                    var events = JsonSerializer.Deserialize<Event>(cached.ToString());

                    _logger.LogInformation("return {keyForRequest}", keyForRequest);

                    return events;
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
            var keyForRequest = CacheKeys.EventById(id);

            try
            {
                var ttl = Convert.ToInt32(_configuration["Redis:ShortTTL"]);
                await _db.StringSetAsync(keyForRequest, JsonSerializer.Serialize<Event>(@event), TimeSpan.FromMinutes(ttl));
                _logger.LogInformation("set {keyForRequest}", keyForRequest);
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
            var keyForRequest = CacheKeys.EventById(id);

            try
            {
                await _db.KeyDeleteAsync(keyForRequest);
                _logger.LogInformation("delete {keyForRequest}", keyForRequest);

            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.ToString());
            }
        }

        public async Task<List<TopEvent>?> GetTop(int number)
        {
            try
            {
                var keyForRequest = CacheKeys.TopEvents(number);

                RedisValue cached = await _db.StringGetAsync(keyForRequest);

                if (cached.HasValue)
                {
                    var events = JsonSerializer.Deserialize<List<TopEvent>>(cached.ToString());

                    _logger.LogInformation("get {keyForRequest}", keyForRequest);

                    return events;
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.ToString());
                return null;
            }

        }

        public async Task<List<TopEvent>?> SetTop(int number, List<TopEvent> events)
        {
            try
            {
                var keyForRequest = CacheKeys.TopEvents(number);

                var ttl = Convert.ToInt32(_configuration["Redis:LongTTL"]);

                await _db.StringSetAsync(keyForRequest, JsonSerializer.Serialize<List<TopEvent>>(events), TimeSpan.FromMinutes(ttl));
                _logger.LogInformation("set {keyForRequest}", keyForRequest);
                return events;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.ToString());
                return null;
            }

        }
    }

    public static class CacheKeys
    {
        private const string EventsPrefix = "events:";

        public static string EventById(Guid id) => $"{EventsPrefix}{id}";
        public static string TopEvents(int count) => $"{EventsPrefix}top{count}";
    }
}
