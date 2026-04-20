using System.Text.Json;

namespace Core.CrossCuttingConcerns.Exceptions
{
    // Sistemde bir hata olduğunda dış dünyaya fırlatacağımız standart JSON şablonu
    public class ErrorDetails
    {
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;

        // Sınıfı doğrudan JSON metnine çeviren sihirli metodumuz
        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}