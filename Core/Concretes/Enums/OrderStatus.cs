namespace Core.Concretes.Enums
{
    // Siparişin yaşam döngüsünü belirleyen sabit listesi
    public enum OrderStatus
    {
        Pending = 1,    // Sipariş alındı, mutfak onayını bekliyor
        Preparing = 2,  // Mutfak hazırlamaya başladı
        Ready = 3,      // Mutfak ürünü bitirdi (Bu aşamada stok düşecek)
        Delivered = 4,  // Garson ürünü masaya götürdü
        Completed = 5,  // Ödeme alındı, adisyon kapandı
        Canceled = 6    // Herhangi bir sebeple iptal edildi
    }
}