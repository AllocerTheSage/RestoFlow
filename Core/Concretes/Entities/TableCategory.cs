using Core.Abstracts.Bases;

namespace Core.Concretes.Entities
{
    // İŞTE BURASI: Sadece "class" yerine "public class" olmalı!
    public class TableCategory : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        public virtual ICollection<Table> Tables { get; set; } = new List<Table>();
    }
}