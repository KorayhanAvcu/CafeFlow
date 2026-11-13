using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafeOrder.DataAccessLayer.Migrations
{
    /// <summary>
    /// Veri migration'ı: "Krema" seçeneğini (Cream grubu, +15 ₺) ekler. Tablo yapısı değişmez.
    /// Boş veritabanında hiçbir şey yapmaz; orada seçenekleri (Krema dahil) SeedData ekler.
    /// Böylece seçenek Id'leri yeni ve eski veritabanında aynı sırada kalır (Krema = 10).
    /// </summary>
    public partial class AddCreamOption : Migration
    {
        /// <summary>Daha önce doldurulmuş bir veritabanında Krema yoksa ekler.</summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM ProductOptions)
   AND NOT EXISTS (SELECT 1 FROM ProductOptions WHERE GroupKey = N'Cream')
    INSERT INTO ProductOptions (GroupKey, Value, PriceDelta, IsDefault) VALUES (N'Cream', N'Krema', 15, 0);");
        }

        /// <summary>Krema seçeneğini siparişlerde kullanılmamışsa siler.</summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE FROM ProductOptions
WHERE GroupKey = N'Cream'
  AND NOT EXISTS (SELECT 1 FROM OrderDetailOptions o WHERE o.ProductOptionId = ProductOptions.Id);");
        }
    }
}
