using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndyMindy
{
    public class PlannedProductionItem
    {
        public int TypeID { get; set; }
        public string Name { get; set; }
        public int Quantity { get; set; }
        public DateTime PlannedDate { get; set; } = DateTime.Now;
    }

    public class ProductionPlanner
    {
        public List<PlannedProductionItem> PlannedItems { get; set; } = new();

        public void AddItem(PlannedProductionItem item)
        {
            PlannedItems.Add(item);
        }

        public void RemoveItem(PlannedProductionItem item)
        {
            PlannedItems.Remove(item);
        }

        public void Clear()
        {
            PlannedItems.Clear();
        }
    }
}
