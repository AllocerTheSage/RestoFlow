namespace Core.Abstracts
{
    // Hem işlem sonucunu (Başarı/Mesaj) hem de beraberinde gelen veriyi (Data) tutar.
    // T: Gelecek verinin tipini temsil eder (Örn: Product, List<Order> vb.)
    public interface IDataResult<T> : IResult
    {
        T Data { get; }
    }
}