using System.Collections.Generic;
using System.Data.SQLite;
using System.Windows.Media.Media3D;
using Dapper;

public static class Database
{
    private const string ConnectionString = "Data Source=blueprints.db";

    public static List<Blueprint> LoadBlueprints()
    {
        using var connection = new SQLiteConnection(ConnectionString);
        var sql = @"
            SELECT 
            p.productTypeID AS ProductID, 
            p.typeID AS BlueprintID,
            t.typeName AS Name, 
            a.time AS ProductionTime
            FROM industryActivityProducts p
            JOIN invTypes t ON t.typeID = p.productTypeID
            JOIN industryActivity a ON a.typeID = p.typeID AND a.activityID = 1
            WHERE p.activityID = 1";
        return connection.Query<Blueprint>(sql).AsList();
    }

    public static List<BlueprintMaterial> LoadMaterials(int blueprintTypeId)
    {
        using var connection = new SQLiteConnection("Data Source=blueprints.db");
        connection.Open();

        var sql = @"
        SELECT
            m.materialTypeID AS MaterialTypeID,
            t.typeName AS MaterialName,
            m.quantity AS Quantity
        FROM industryActivityMaterials m
        JOIN invTypes t ON t.typeID = m.materialTypeID
        WHERE m.activityID = 1 AND m.typeID = @TypeID";

        var materials = connection.Query<BlueprintMaterial>(sql, new { TypeID = blueprintTypeId }).ToList();

        foreach (var material in materials)
        {
            material.IsBuildable = false;
            material.BuildInsteadOfBuy = false;

            var nestedBlueprint = LoadBlueprintByProductID(material.MaterialTypeID, connection);
            if (nestedBlueprint != null)
            {
                material.IsBuildable = true;
                material.NestedBlueprint = nestedBlueprint;
            }
        }

        return materials;
    }

    private static Blueprint? LoadBlueprintByProductID(int productTypeId, SQLiteConnection conn)
    {
        const string sql = @"
        SELECT 
            p.productTypeID AS ProductID,
            p.typeID AS BlueprintID,
            t.typeName AS Name,
            a.time AS ProductionTime
        FROM industryActivityProducts p
        JOIN invTypes t ON t.typeID = p.productTypeID
        LEFT JOIN industryActivity a ON a.typeID = p.typeID AND a.activityID = 1
        WHERE p.productTypeID = @productTypeId
        LIMIT 1";

        var bp = conn.QueryFirstOrDefault<Blueprint>(sql, new { productTypeId });
        if (bp != null)
        {
            bp.Materials = LoadMaterialsRecursive(bp.BlueprintID, conn); // ← Fill materials directly
        }

        return bp;
    }

    private static List<BlueprintMaterial> LoadMaterialsRecursive(int blueprintTypeId, SQLiteConnection conn)
    {
        var sql = @"
        SELECT
            m.materialTypeID AS MaterialTypeID,
            t.typeName AS MaterialName,
            m.quantity AS Quantity
        FROM industryActivityMaterials m
        JOIN invTypes t ON t.typeID = m.materialTypeID
        WHERE m.activityID = 1 AND m.typeID = @TypeID";

        var materials = conn.Query<BlueprintMaterial>(sql, new { TypeID = blueprintTypeId }).ToList();

        foreach (var material in materials)
        {
            material.IsBuildable = false;
            material.BuildInsteadOfBuy = false;

            var nested = LoadBlueprintByProductID(material.MaterialTypeID, conn);
            if (nested != null)
            {
                material.IsBuildable = true;
                material.NestedBlueprint = nested;
            }
        }

        return materials;
    }

}
