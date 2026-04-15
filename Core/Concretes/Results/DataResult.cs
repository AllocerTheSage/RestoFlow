using Core.Abstracts;

namespace Core.Concretes.Results
{
    // T: Taşınacak verinin tipini temsil eder.
    // Result sınıfından miras alarak Başarı ve Mesaj özelliklerini kullanıyoruz.
    public class DataResult<T> : Result, IDataResult<T>
    {
        // Constructor: Veri, başarı durumu ve mesajı üst sınıfa gönderir.
        public DataResult(T data, bool success, string message) : base(success, message)
        {
            Data = data;
        }

        // Sadece veri ve başarı durumu içeren kullanım.
        public DataResult(T data, bool success) : base(success, string.Empty)
        {
            Data = data;
        }

        // Taşınan asıl veri (Örn: Ürün nesnesi veya Ürün listesi).
        public T Data { get; }
    }
}