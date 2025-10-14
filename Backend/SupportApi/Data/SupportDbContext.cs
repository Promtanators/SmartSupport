using Microsoft.EntityFrameworkCore;
using SupportApi.Models.Dto;


namespace SupportApi.Data;

public class SupportDbContext : DbContext
{
    public SupportDbContext(DbContextOptions<SupportDbContext> options)
        : base(options)
    {
        Database.EnsureCreated();
    }
}