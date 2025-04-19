using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

public static class PriceProvider
{
    private const int REGION_ID = 10000002;        // The Forge
    private const long JITA_ID = 60003760;         // Jita IV - Moon 4 - Caldari Navy Assembly Plant
    private const string CacheFile = "market_prices_cache.json";
    private const string EsiPriceUrl = "https://esi.evetech.net/latest/markets/prices/";

    private static readonly HttpClient http = new();
    private static List<MarketEstimate> priceEstimates;

    // ✅ Public: Get average market value (cached hourly)
    public static async Task<decimal> GetEstimatedItemValueAsync(int typeId)
    {
        if (priceEstimates == null)
            await LoadOrRefreshPriceCacheAsync();

        if (priceEstimates == null)
            return 0;

        var match = priceEstimates.FirstOrDefault(p => p.type_id == typeId);
        return match?.average_price ?? 0;
    }

    // ✅ Public: Get real-time Buy/Sell prices from Jita
    public static async Task<(decimal Buy, decimal Sell)> GetPricesAsync(int typeId)
    {
        var url = $"https://esi.evetech.net/latest/markets/{REGION_ID}/orders/?type_id={typeId}";
        var orders = await http.GetFromJsonAsync<List<MarketOrder>>(url);

        if (orders == null || !orders.Any())
            return (0m, 0m);

        var jitaOrders = orders.Where(o => o.location_id == JITA_ID).ToList();

        var sell = jitaOrders
            .Where(o => !o.is_buy_order)
            .OrderBy(o => o.price)
            .FirstOrDefault();

        var buy = jitaOrders
            .Where(o => o.is_buy_order)
            .OrderByDescending(o => o.price)
            .FirstOrDefault();

        return (buy?.price ?? 0m, sell?.price ?? 0m);
    }

    // ✅ Caching logic
    private static async Task LoadOrRefreshPriceCacheAsync()
    {
        bool needsRefresh = true;

        if (File.Exists(CacheFile))
        {
            var lastModified = File.GetLastWriteTimeUtc(CacheFile);
            if ((DateTime.UtcNow - lastModified).TotalMinutes < 60)
            {
                using var stream = File.OpenRead(CacheFile);
                priceEstimates = await JsonSerializer.DeserializeAsync<List<MarketEstimate>>(stream);
                needsRefresh = false;
            }
        }

        if (needsRefresh)
        {
            priceEstimates = await http.GetFromJsonAsync<List<MarketEstimate>>(EsiPriceUrl);

            using var outStream = File.Create(CacheFile);
            await JsonSerializer.SerializeAsync(outStream, priceEstimates);
        }
    }

    // ✅ Models
    public class MarketEstimate
    {
        public int type_id { get; set; }
        public decimal? average_price { get; set; }
        public decimal? adjusted_price { get; set; }
    }

    public class MarketOrder
    {
        public long order_id { get; set; }
        public int type_id { get; set; }
        public long location_id { get; set; }
        public bool is_buy_order { get; set; }
        public decimal price { get; set; }
        public long volume_remain { get; set; }
        public string issued { get; set; }
    }
}