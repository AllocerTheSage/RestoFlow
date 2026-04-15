namespace Core.Concretes.Results
{
    // İşlem başarılı olduğunda kullanılacak olan sınıf.
    // Miras aldığı Result sınıfına her zaman 'true' (başarılı) bilgisini gönderir.
    public class SuccessResult : Result
    {
        // Mesajlı kullanım
        public SuccessResult(string message) : base(true, message)
        {
        }

        // Mesajsız kullanım (Sadece başarılı bilgisi döner)
        public SuccessResult() : base(true, string.Empty)
        {
        }
    }
}