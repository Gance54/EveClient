using System.Collections.Generic;
using System.Data.SQLite;
using System.Windows.Media.Media3D;
using Dapper;

public static class Database
{
    private const string ConnectionString = "Data Source=blueprints.db";

    public static List<Blueprint> LoadBlueprints()
    {
        using var connection = new SQLiteConnection("Data Source=blueprints.db");

        var sql = @"
        SELECT 
            p.productTypeID AS ProductID,
            p.typeID AS BlueprintID,
            t.typeName AS Name,
            a.time AS ProductionTime,
            p.quantity AS ProductQuantity
        FROM industryActivityProducts p
        JOIN invTypes t ON t.typeID = p.productTypeID
        JOIN industryActivity a ON a.typeID = p.typeID AND a.activityID = p.activityID
        WHERE p.activityID IN (1, 11)";

        return connection.Query<Blueprint>(sql).AsList();
    }


    public static List<BlueprintMaterial> LoadMaterials(int blueprintTypeId)
    {
        using var connection = new SQLiteConnection(ConnectionString);
        connection.Open();

        return LoadMaterialsRecursive(blueprintTypeId, connection, new Stack<int>());
    }

    private static Blueprint? LoadBlueprintByProductID(int productTypeId, SQLiteConnection conn, Stack<int> path)
    {
        const string sql = @"
            SELECT 
                p.productTypeID AS ProductID,
                p.typeID AS BlueprintID,
                t.typeName AS Name,
                a.time AS ProductionTime,
                p.quantity AS ProductQuantity
            FROM industryActivityProducts p
            JOIN invTypes t ON t.typeID = p.productTypeID
            LEFT JOIN industryActivity a ON a.typeID = p.typeID AND a.activityID = 1
            WHERE p.productTypeID = @productTypeId
            LIMIT 1";

        var bp = conn.QueryFirstOrDefault<Blueprint>(sql, new { productTypeId });

        if (bp != null)
        {
            bp.Materials = LoadMaterialsRecursive(bp.BlueprintID, conn, path);
        }

        return bp;
    }
    private static List<BlueprintMaterial> LoadMaterialsRecursive(int blueprintTypeId, SQLiteConnection conn, Stack<int> path)
    {
        // prevent cyclic recursion
        if (path.Contains(blueprintTypeId))
            return new List<BlueprintMaterial>();

        path.Push(blueprintTypeId);

        var sql = @"
            SELECT
                m.materialTypeID AS MaterialTypeID,
                t.typeName AS MaterialName,
                m.quantity AS Quantity
            FROM industryActivityMaterials m
            JOIN invTypes t ON t.typeID = m.materialTypeID
            WHERE m.activityID IN (1, 11) AND m.typeID = @TypeID";

        var materials = conn.Query<BlueprintMaterial>(sql, new { TypeID = blueprintTypeId }).ToList();

        foreach (var material in materials)
        {
            material.IsBuildable = false;
            material.BuildInsteadOfBuy = false;

            var nested = LoadBlueprintByProductID(material.MaterialTypeID, conn, path);
            if (nested != null)
            {
                material.IsBuildable = true;
                material.NestedBlueprint = nested;
            }
            System.Diagnostics.Debug.WriteLine($"MATERIAL: {material.MaterialName} ({material.MaterialTypeID}) → Buildable={material.IsBuildable}");
        }

        path.Pop();

        return materials;
    }

}
