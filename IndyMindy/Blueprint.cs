using System;

public class Blueprint
{
    public int ProductID { get; set; }     // The output item (e.g., Vexor)
    public int BlueprintID { get; set; }   // The blueprint type ID
    public string Name { get; set; }
    public int ProductionTime { get; set; }
    public int ProductQuantity { get; set; } = 1;
    public List<BlueprintMaterial> Materials { get; set; } = new();

    public string BPIconUrl => $"https://images.evetech.net/types/{BlueprintID}/icon";
    public string ItemconUrl => $"https://images.evetech.net/types/{ProductID}/icon";
}

public class BlueprintMaterial
{
    public int MaterialTypeID { get; set; }
    public string MaterialName { get; set; }
    public int Quantity { get; set; }
    public int DisplayQuantity { get; set; }
    public bool IsBuildable { get; set; }
    public bool BuildInsteadOfBuy { get; set; }
    public Blueprint? NestedBlueprint { get; set; }
    public int Depth { get; set; }
    public string IconUrl => $"https://images.evetech.net/types/{MaterialTypeID}/icon";
}

public class MaterialSummary
{
    public string MaterialName { get; set; }
    public int Quantity { get; set; }
    public decimal BuyPrice { get; set; }
    public decimal SellPrice { get; set; }
    public int TypeID { get; set; }
    public decimal TotalBuy => BuyPrice * Quantity;
    public decimal TotalSell => SellPrice * Quantity;
}