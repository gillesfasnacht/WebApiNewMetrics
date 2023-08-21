using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Sinks.OpenTelemetry;
using WebApiNewMetrics;

var builder = WebApplication.CreateBuilder(args);

const string outputTemplate =
    "[{Level:w}]: {Timestamp:dd-MM-yyyy:HH:mm:ss} {MachineName} {EnvironmentName} {SourceContext} {Message}{NewLine}{Exception}";

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .Enrich.WithThreadId()
    .Enrich.WithEnvironmentName()
    .Enrich.WithMachineName()
    .WriteTo.Console(outputTemplate: outputTemplate)
    .WriteTo.OpenTelemetry(options =>
    {
        options.Endpoint = "http://host.docker.internal:4317";
        options.Protocol = OtlpProtocol.Grpc;
        options.ResourceAttributes = new Dictionary<string, object>
        {
            ["app"] = "webapi",
            ["runtime"] = "dotnet",
            ["service.name"] = "WebApiNewMetrics"
        };
    })
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddOpenTelemetry()
    .WithMetrics(builder =>
    {
        builder.AddPrometheusExporter();

        builder.AddMeter("Microsoft.AspNetCore.Hosting", "Microsoft.AspNetCore.Server.Kestrel");
        builder.AddView("request-duration",
            new ExplicitBucketHistogramConfiguration
            {
                Boundaries = new double[] { 0, 0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10 }
            });
    })
    .WithTracing(tracerProviderBuilder =>
        tracerProviderBuilder
            .AddSource(Instrumentor.ServiceName)
            .ConfigureResource(resource => resource
                .AddService(Instrumentor.ServiceName))
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
                options.Endpoint = new Uri("http://host.docker.internal:4317");
                options.Protocol = OtlpExportProtocol.Grpc;
            }));

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMetrics();

var app = builder.Build();

app.MapPrometheusScrapingEndpoint();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
} else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.UseSerilogRequestLogging();

app.MapControllers();

app.Run();
