
namespace OrderFlow.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderFlow.Domain.Orders;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        // Strongly-typed id <-> Guid value converter.
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id)
            .HasConversion(id => id.Value, value => new OrderId(value))
            .ValueGeneratedNever();

        builder.Property(o => o.CustomerId).HasMaxLength(100).IsRequired();
        builder.Property(o => o.Currency).HasMaxLength(3).IsRequired();
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(o => o.CreatedAtUtc);

        // Optimistic concurrency via SQL rowversion.
        builder.Property(o => o.Version).IsRowVersion();

        // Address as an owned value object (columns inline on Orders table).
        builder.OwnsOne(o => o.ShippingAddress, a =>
        {
            a.Property(p => p.Line1).HasColumnName("ShipLine1").HasMaxLength(200);
            a.Property(p => p.City).HasColumnName("ShipCity").HasMaxLength(100);
            a.Property(p => p.PostalCode).HasColumnName("ShipPostalCode").HasMaxLength(20);
            a.Property(p => p.Country).HasColumnName("ShipCountry").HasMaxLength(100);
        });

        // Lines as an owned collection (separate table, part of the aggregate).
        builder.OwnsMany(o => o.Lines, l =>
        {
            l.ToTable("OrderLines");
            l.WithOwner().HasForeignKey("OrderId");
            l.HasKey(x => x.Id);
            l.Property(x => x.Sku).HasMaxLength(64).IsRequired();
            l.Property(x => x.Quantity);
            l.OwnsOne(x => x.UnitPrice, m =>
            {
                m.Property(p => p.Amount).HasColumnName("UnitPriceAmount").HasPrecision(18, 2);
                m.Property(p => p.Currency).HasColumnName("UnitPriceCurrency").HasMaxLength(3);
            });
        });

        builder.Navigation(o => o.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Total is a computed property — do not map it.
        builder.Ignore(o => o.Total);
        builder.Ignore(o => o.DomainEvents);
    }
}
