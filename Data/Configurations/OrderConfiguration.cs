using Core.Concretes.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Configurations
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.HasKey(x => x.Id);

            // Masa numarası veya sipariş notu gibi alanlar için sınırlama
            builder.Property(x => x.OrderNumber).IsRequired().HasMaxLength(50);

            builder.Property(x => x.TotalPrice).IsRequired();

            // Bir siparişin birden fazla kalem ürünü (OrderItem) olabilir.
            // Bu ilişkiyi burada tanımlıyoruz:
            builder.HasMany(x => x.OrderItems)
                   .WithOne(x => x.Order)
                   .HasForeignKey(x => x.OrderId)
                   .OnDelete(DeleteBehavior.Cascade); // Sipariş silinirse içindeki yemekler de silinsin.

            builder.ToTable("Orders");
        }
    }
}