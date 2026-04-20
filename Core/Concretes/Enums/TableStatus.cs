namespace Core.Concretes.Enums
{
    // Bir masanın alabileceği durumları temsil eder.
    public enum TableStatus
    {
        Empty = 1,    // Masa Boş (Yeşil yanar)
        Occupied = 2, // Masa Dolu/Müşteri oturuyor (Kırmızı yanar)
        Reserved = 3  // Masa Rezerve (Sarı yanar)
    }
}