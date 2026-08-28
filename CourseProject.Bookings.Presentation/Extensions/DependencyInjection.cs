using CourseProject.Bookings.Presentation.Interfaces;
using CourseProject.Bookings.Presentation.Services;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace CourseProject.Bookings.Presentation.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IBookingModelDtoMapperService, BookingModelDtoMapperService>();

            services.AddOpenTelemetry()
                .ConfigureResource(r => r.AddService(serviceName: "bookings-service")) // доступен по /metrics
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
