using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Concretes.Enums
{
    public enum LogType
    {
        UserLogin = 1,
        UserLogout = 2,
        RoleChanged = 3,
        SecurityBreach = 4,

        OrderCancelled = 5,
        ItemComplimentary = 6,

        PriceUpdated = 7,
        StockStatusChanged = 8,

        TableLayoutChanged = 9,
        TableTransferred = 10,
        TableNameOrCategoryChanged = 11,
        TableReservationChanged = 12,

    }
}
