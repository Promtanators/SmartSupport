using Microsoft.EntityFrameworkCore;
using SupportApi.Data;
using SupportApi.Models.Entities;

var envFile = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), ".env"));
Console.WriteLine(envFile);
if (File.Exists(envFile))
    foreach (var l in File.ReadLines(envFile))
    {
        if (string.IsNullOrWhiteSpace(l) || l.StartsWith("#") || !l.Contains('=')) continue;
        var p = l.Split('=', 2);
        Environment.SetEnvironmentVariable(p[0].Trim(), p[1].Trim());
    }


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var loggerFactory = LoggerFactory.Create(b =>
    {
        b.AddConsole();
        b.AddDebug();
    }
);

Logger.Initialization(loggerFactory.CreateLogger("Global"));
var connection = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<SupportDbContext>(options =>
    options.UseSqlite(connection));

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});


var app = builder.Build();


app.UseSwagger();
app.UseSwaggerUI();

// if (app.Environment.IsDevelopment())
// {
//     app.UseSwagger();
//     app.UseSwaggerUI();
// }

app.UseCors();

app.MapControllers();

// setx SCIBOX_API_KEY "TOKEN" - Windows
// export SCIBOX_API_KEY="TOKEN" - Linux/macOS

app.Run();