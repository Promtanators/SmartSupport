using Microsoft.EntityFrameworkCore;
using SupportApi.Models.Dto;
using SupportApi.Models.Entities;


namespace SupportApi.Data;

public class SupportDbContext : DbContext
{
    public SupportDbContext(DbContextOptions<SupportDbContext> options)
        : base(options)
    {
        Database.EnsureCreated();
    }

    public DbSet<BankFaq> BankFaqs { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BankFaq>(entity =>
        {
            entity.ToTable("BankFaqs");
            entity.HasKey(f => f.ExampleQuestion);
            
            entity.Property(f => f.MainCategory)
                .HasColumnName("Основная категория");

            entity.Property(f => f.Subcategory)
                .HasColumnName("Подкатегория");

            entity.Property(f => f.ExampleQuestion)
                .HasColumnName("Пример вопроса");

            entity.Property(f => f.Priority)
                .HasColumnName("Приоритет");

            entity.Property(f => f.TargetAudience)
                .HasColumnName("Целевая аудитория");

            entity.Property(f => f.TemplateResponse)
                .HasColumnName("Шаблонный ответ");
        });
    }
}