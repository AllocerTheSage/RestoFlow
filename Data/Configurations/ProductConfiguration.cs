using Core.Concretes.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Configurations
{
    // Product tablosunun veritabanı kurallarını belirliyoruz.
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            // Primary Key (Zaten BaseEntity'den geliyor ama netleştirebiliriz)
            builder.HasKey(x => x.Id);

            // Name alanı zorunlu ve en fazla 150 karakter olsun
            builder.Property(x => x.Name).IsRequired().HasMaxLength(150);

            // Price alanı zorunlu ve ondalıklı formatta olsun
            // SQLite'da ondalıklar için genellikle "decimal" tipi kullanılır.
            builder.Property(x => x.Price).IsRequired();

            // Stock alanı zorunlu
            builder.Property(x => x.Stock).IsRequired();

            // Tablo ismi (Opsiyonel, belirtmezsek 'Products' olur)
            builder.ToTable("Products");
        }
    }
}