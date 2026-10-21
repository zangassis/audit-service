using AuditService.Data;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<AuditDbContext>(options => 
{ 
    options
        .UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"), 
        npgsqlOptions => { 
            npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorCodesToAdd: null); 
        }); 
});

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("audit", limiterOptions =>
    {
        limiterOptions.PermitLimit = 5;
        limiterOptions.Window = TimeSpan.FromSeconds(1);
        limiterOptions.QueueLimit = 0;
    });
});

var app = builder.Build();

app.UseRateLimiter();

app.MapControllers();

app.Run();