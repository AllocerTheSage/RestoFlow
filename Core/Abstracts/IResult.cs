namespace Core.Abstracts
{
    // API'den dönecek tüm yanıtların ortak iskeletini belirler.
    public interface IResult
    {
        // İşlemin başarı durumunu tutar (True/False).
        bool Success { get; }

        // İşlem sonucu kullanıcıya gösterilecek mesajı tutar (Örn: "Ürün başarıyla eklendi").
        string Message { get; }
    }
}