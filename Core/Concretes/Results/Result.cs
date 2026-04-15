using Core.Abstracts;

namespace Core.Concretes.Results
{
    // IResult arayüzünden miras alarak somut bir temel oluşturuyoruz.
    public class Result : IResult
    {
        // Constructor (Yapıcı Metot): Sınıf oluşturulurken değerleri zorunlu tutuyoruz.
        public Result(bool success, string message)
        {
            Success = success;
            Message = message;
        }

        // Sadece okunabilir (get) özellikler
        public bool Success { get; }
        public string Message { get; }
    }
}