using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderFlow.Infrastructure.Domain;

namespace OrderFlow.Infrastructure.Persistence.Configurations;

public sealed class OrderTableConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");
        builder.HasKey(o => o.Id);
        
        builder.Property(o => o.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(o => o.CustomerName)
            .HasColumnName("customer_name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(o => o.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(o => o.TotalAmount)
            .HasColumnName("total_amount")
            .HasPrecision(18, 2);

        builder.Property(o => o.CreatedAt)
            .HasColumnName("created_at");

        builder.HasIndex(o => o.Status)
            .HasDatabaseName("ix_orders_status");
    }
}