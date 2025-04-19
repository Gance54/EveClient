using System;

public class Blueprint
{
    public int ProductID { get; set; }     // The output item (e.g., Vexor)
    public int BlueprintID { get; set; }   // The blueprint type ID
    public string Name { get; set; }
    public int ProductionTime { get; set; }

    public string BPIconUrl => $"https://images.evetech.net/types/{BlueprintID}/icon";
    public string ItemconUrl => $"https://images.evetech.net/types/{ProductID}/icon";
}

public class BlueprintMaterial
{
    public int MaterialTypeID { get; set; }
    public string MaterialName { get; set; }
    public int Quantity { get; set; }
    public string IconUrl => $"https://images.evetech.net/types/{MaterialTypeID}/icon";
}