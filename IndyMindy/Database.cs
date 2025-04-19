using System.Collections.Generic;
using System.Data.SQLite;
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
        System.Diagnostics.Debug.WriteLine($"Loading mats");
        using var connection = new SQLiteConnection("Data Source=blueprints.db");

        var sql = @"
        SELECT
            m.materialTypeID AS MaterialTypeID,
            t.typeName AS MaterialName,
            m.quantity AS Quantity
        FROM industryActivityMaterials m
        JOIN invTypes t ON t.typeID = m.materialTypeID
        WHERE m.activityID = 1 AND m.typeID = @TypeID";

        return connection.Query<BlueprintMaterial>(sql, new { TypeID = blueprintTypeId }).AsList();
    }
}
