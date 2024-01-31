using Microsoft.Extensions.Configuration;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.OpenTelemetry;
using WebApiNewMetrics;

internal class Program
{
    public static ConfigurationManager? Configuration { get; private set; }

    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .Enrich.WithThreadId()
            .Enrich.WithEnvironmentName()
            .Enrich.WithMachineName()
            .WriteTo.Console()
            .WriteTo.OpenTelemetry(options =>
            {
                options.Endpoint = builder.Configuration["Otlp:Endpoint"] ?? throw new InvalidOperationException();
                options.Protocol = OtlpProtocol.Grpc;
                options.ResourceAttributes = new Dictionary<string, object>
                {
                    ["app"] = builder.Configuration["Otlp:App"] ?? throw new InvalidOperationException(),
                    ["runtime"] = builder.Configuration["Otlp:Runtime"] ?? throw new InvalidOperationException(),
                    ["service.name"] = builder.Configuration["Otlp:ServiceName"] ?? throw new InvalidOperationException()
                };
            })
            .CreateLogger();

        builder.Host.UseSerilog();

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metricsProviderBuilder =>
            {
                metricsProviderBuilder.AddAspNetCoreInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(builder.Configuration["Otlp:Endpoint"] ?? throw new InvalidOperationException());
                        options.Protocol = OtlpExportProtocol.Grpc;
                    });
            })
            .WithTracing(tracerProviderBuilder =>
            {
                tracerProviderBuilder.AddSource(Instrumentor.ServiceName)
                    .ConfigureResource(resource =>
                        resource.AddService(Instrumentor.ServiceName))
                            .AddAspNetCoreInstrumentation()
                            .AddHttpClientInstrumentation(options =>
                            {
                                options.FilterHttpRequestMessage = req =>
                                {
                                    var ignore = new[] { "/loki/api" };
                                    return !ignore.Any(s => req.RequestUri!.ToString().Contains(s));
                                };
                            })
                            .AddOtlpExporter(options =>
                            {
                                options.Endpoint = new Uri(builder.Configuration["Otlp:Endpoint"] ?? throw new InvalidOperationException());
                                options.Protocol = OtlpExportProtocol.Grpc;
                            });
            });
                
        // Add services to the container.
        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddMetrics();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        else
        {
            app.UseHsts();
            app.UseHttpsRedirection();
        }

        app.UseAuthorization();

        app.UseSerilogRequestLogging();

        app.MapControllers();

        app.Run();
    }
}