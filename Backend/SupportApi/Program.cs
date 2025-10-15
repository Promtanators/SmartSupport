using Microsoft.EntityFrameworkCore;
using SupportApi.Data;
using SupportApi.Models.Entities;

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