namespace Core.Concretes.Results
{
    // İşlem başarılı olduğunda ve veri döndürüleceğinde kullanılır.
    // Miras aldığı DataResult sınıfına her zaman 'true' (başarılı) bilgisini gönderir.
    public class SuccessDataResult<T> : DataResult<T>
    {
        // Veri ve mesaj içeren kullanım
        public SuccessDataResult(T data, string message) : base(data, true, message)
        {
        }

        // Sadece veri içeren kullanım
        public SuccessDataResult(T data) : base(data, true)
        {
        }

        // Sadece mesaj içeren (varsayılan veri tipiyle) kullanım
        public SuccessDataResult(string message) : base(default!, true, message)
        {
        }

        // Hiçbir şey içermeyen boş başarılı kullanım
        public SuccessDataResult() : base(default!, true)
        {
        }
    }
}