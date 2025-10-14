using Microsoft.EntityFrameworkCore;
using SupportApi;
using SupportApi.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connection = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<SupportDbContext>(options =>
    options.UseSqlite(connection));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

// setx SCIBOX_API_KEY "TOKEN" - Windows
// export SCIBOX_API_KEY="TOKEN" - Linux/macOS

app.Run();


