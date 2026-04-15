namespace Core.Concretes.Results
{
    // İşlem başarısız olduğunda ve (istenirse) veri veya hata mesajı döndürüleceğinde kullanılır.
    // Miras aldığı DataResult sınıfına her zaman 'false' (başarısız) bilgisini gönderir.
    public class ErrorDataResult<T> : DataResult<T>
    {
        // Veri ve mesaj içeren kullanım
        public ErrorDataResult(T data, string message) : base(data, false, message)
        {
        }

        // Sadece veri içeren kullanım
        public ErrorDataResult(T data) : base(data, false)
        {
        }

        // Sadece mesaj içeren kullanım
        public ErrorDataResult(string message) : base(default!, false, message)
        {
        }

        // Hiçbir şey içermeyen boş başarısız kullanım
        public ErrorDataResult() : base(default!, false)
        {
        }
    }
}