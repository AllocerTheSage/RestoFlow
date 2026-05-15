using Core.Abstracts.Bases;
using Core.Concretes.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Concretes.Entities
{
    public class SystemLog : BaseEntity
    {
        public LogType LogType { get; set; }
        public string? UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual AppUser? User { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Details { get; set; }
        public int? RelatedEntityId { get; set; }

    }
}
