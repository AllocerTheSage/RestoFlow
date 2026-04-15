namespace Core.Constants
{
    public static class Permissions
    {
        // 1. KATEGORİ: OPERASYONEL AKIŞ (GARSON & MUTFAK)
        // Bu bölüm garson ile mutfak arasındaki dijital köprüyü kuran yetkileri içerir.
        public static class Operations
        {
            // Garsonun menüde hangi üründen kaç tane kaldığını sayısal olarak görmesini sağlar.
            // [Stok Miktarı Görüntüleme]
            public const string ViewStockCount = "Permissions.Operations.ViewStockCount";

            // Masaya sipariş girişi yapma ve bu siparişi mutfak ekranına düşürme yetkisidir.
            // [Sipariş Oluşturma]
            public const string CreateOrder = "Permissions.Operations.CreateOrder";

            // Mutfağın siparişi onaylayıp stoktan düşürmesini sağlayan "Eksilt (-)" butonu yetkisidir.
            // [Stoktan Düşme Onayı]
            public const string ConfirmAndDeductStock = "Permissions.Operations.ConfirmAndDeductStock";

            // Mutfak personeline bir ürünü (fırın arızası vb. nedenlerle) satışa kapatma gücü verir.
            // [Ürünü Satışa Kapatma]
            public const string ToggleAvailability = "Permissions.Operations.ToggleAvailability";

            // Siparişin mutfakta hangi aşamada (Hazırlanıyor, Hazır vb.) olduğunu görme ve güncelleme yetkisidir.
            // [Sipariş Durumu Takibi]
            public const string TrackOrderStatus = "Permissions.Operations.TrackOrderStatus";
        }

        // 2. KATEGORİ: ADİSYON VE MASA YÖNETİMİ
        // Masaların düzeni ve adisyon üzerindeki kritik düzeltmeleri kapsar.
        public static class TableManagement
        {
            // Yanlış girilen bir ürünü adisyon kapanmadan listeden çıkarma yetkisidir.
            // [Adisyondan Ürün Silme]
            public const string DeleteProduct = "Permissions.Table.DeleteProduct";

            // Bir masadaki hesabı başka masaya aktarma veya masaları birleştirme yetkisidir.
            // [Masa Taşıma ve Birleştirme]
            public const string TransferOrder = "Permissions.Table.Transfer";

            // Tüm masayı tek seferde iptal etme yetkisidir (Suistimale açık olduğu için kritiktir).
            // [Adisyon İptali]
            public const string CancelOrder = "Permissions.Table.Cancel";

            // Müşteriye ikram ürün verme veya hatalı ürünü zayi olarak sisteme girme yetkisidir.
            // [İkram ve Zayi İşleme]
            public const string ComplimentaryAndReturn = "Permissions.Table.ComplimentaryAndReturn";
        }

        // 3. KATEGORİ: FİNANS VE RAPORLAMA
        // Kasa işlemleri ve parayla ilgili tüm yetkiler burada toplanır.
        public static class Finance
        {
            // Masanın ödemesini alıp adisyonu sistemde resmen kapatma yetkisidir.
            // [Ödeme Alma]
            public const string ReceivePayment = "Permissions.Finance.Receive";

            // Hesap toplamı üzerinden indirim uygulama yetkisidir.
            // [İndirim Uygulama]
            public const string ApplyDiscount = "Permissions.Finance.ApplyDiscount";

            // Gün sonu cirosunu ve hangi personelin ne kadar satış yaptığını görme yetkisidir.
            // [Rapor Görüntüleme]
            public const string ViewReports = "Permissions.Finance.ViewReports";
        }

        // 4. KATEGORİ: SİSTEM VE PERSONEL YÖNETİMİ
        // RestoFlow Dashboard'un beyni olan, her şeyi değiştirebilen en üst yetkilerdir.
        public static class Administration
        {
            // Yeni personel ekleme, şifre sıfırlama ve rollere yetki atama yetkisidir.
            // [Personel ve Yetki Yönetimi]
            public const string ManageStaff = "Permissions.Administration.ManageStaff";

            // Menüdeki fiyatları, ürün isimlerini ve stok limitlerini genel olarak düzenleme yetkisidir.
            // [Menü ve Fiyat Yönetimi]
            public const string ManageMenu = "Permissions.Administration.ManageMenu";

            // Restoranın masa yerleşimini ve kat planını tasarlama yetkisidir.
            // [Masa Düzeni Tasarımı]
            public const string ManageLayout = "Permissions.Administration.ManageLayout";
        }
    }
}