using CourseProject.Events.Presentation.Interfaces;
using CourseProject.Events.Presentation.Services;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace CourseProject.Events.Presentation.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IEventModelDtoMapperService, EventModelDtoMapperService>();
            services.AddSingleton<IEventFilterModelDtoMapperService, EventFilterModelDtoMapperService>();
            services.AddSingleton<ITopEventModelDtoMapperService, TopEventModelDtoMapperService>();

            services.AddOpenTelemetry()
                .ConfigureResource(r => r.AddService(serviceName: "events-service")) // доступен по /metrics
                .WithTracing(tracing => tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddOtlpExporter(o => o.Endpoint = new Uri(configuration["Otlp:Endpoint"]!)))
                .WithMetrics(metrics => metrics
                    .AddAspNetCoreInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddPrometheusExporter());


            return services;

        }
    }
}
