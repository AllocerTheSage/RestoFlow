namespace Business.DTOs.PaymentDtos
{
    public class CreatePaymentDto
    {
        // Hangi adisyon için ödeme yapılıyor?
        public int OrderId { get; set; }

        // Müşterinin o an ödediği miktar (Örn: 200 TL)
        public decimal Amount { get; set; }

        // Ödeme yöntemi (Nakit, Kredi Kartı)
        public string PaymentMethod { get; set; } = string.Empty;
    }
}