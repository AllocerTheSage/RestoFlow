namespace Business.DTOs.OrderDtos
{
    // Garsonun tabletten "Masa Değiştir" dediğinde bize göndereceği paket
    public class TransferTableDto
    {
        // Taşınacak olan, halihazırda açık olan siparişin/adisyonun ID'si
        public int OrderId { get; set; }

        // Müşterinin geçmek istediği YENİ masanın ID'si
        public int NewTableId { get; set; }
    }
}