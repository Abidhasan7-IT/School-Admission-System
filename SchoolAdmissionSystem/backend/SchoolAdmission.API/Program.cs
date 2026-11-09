using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Contracts;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.Infrastructure;
using SchoolAdmission.Api.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings__DefaultConnection is not configured.");

builder.Services.AddDbContext<AppDbContext>(o => 
    o.UseNpgsql(connectionString, b => b.MigrationsAssembly("SchoolAdmission.API")));
builder.Services.AddSingleton(new AdmissionOptions(ReadHoldDurationSeconds(builder.Configuration)));
builder.Services.AddScoped<AdmissionService>();
builder.Services.AddHostedService<HoldExpiryService>();

builder.Services.AddControllers().ConfigureApiBehaviorOptions(o =>
    o.InvalidModelStateResponseFactory = _ =>
        new BadRequestObjectResult(new ErrorResponse("VALIDATION_ERROR", "Request body is invalid.")));

builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// Creates tables on first start. Data lives in the PostgreSQL volume, so it survives restarts.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
}

app.UseCors();
app.UseMiddleware<ApiExceptionMiddleware>();
app.MapControllers();
app.Run();

static int ReadHoldDurationSeconds(IConfiguration config) =>
    int.TryParse(config["HOLD_DURATION_SECONDS"], out var seconds) && seconds > 0 ? seconds : 600;

// Lets the integration tests reference the application entry point.
public partial class Program { }
