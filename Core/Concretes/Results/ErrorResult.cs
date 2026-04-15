namespace Core.Concretes.Results
{
    // İşlem başarısız olduğunda kullanılacak olan sınıf.
    // Miras aldığı Result sınıfına her zaman 'false' (başarısız) bilgisini gönderir.
    public class ErrorResult : Result
    {
        // Mesajlı kullanım (Hatanın ne olduğu belirtilir)
        public ErrorResult(string message) : base(false, message)
        {
        }

        // Mesajsız kullanım
        public ErrorResult() : base(false, string.Empty)
        {
        }
    }
}