using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Text.Json;
using System.Net.Http.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));

string connStr = "Data Source=/data/parcels.db";

app.Lifetime.ApplicationStarted.Register(() =>
{
    using var conn = new SqliteConnection(connStr);
    conn.Open();
    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = @"
        CREATE TABLE IF NOT EXISTS parcels (
          id TEXT PRIMARY KEY,
          parcelNumber TEXT,
          ownerName TEXT,
          siteAddress TEXT,
          landValue REAL,
          improvedValue REAL,
          totalValue AS (landValue + improvedValue) STORED,
          latitude REAL,
          longitude REAL
        );";
        cmd.ExecuteNonQuery();
    }

    using var check = conn.CreateCommand();
    check.CommandText = "SELECT COUNT(1) FROM parcels";
    var count = Convert.ToInt32(check.ExecuteScalar());
    if (count == 0)
    {
        var json = File.ReadAllText("/app/seed.json");
        var rows = JsonSerializer.Deserialize<List<Parcel>>(json) ?? new();
        foreach (var p in rows)
        {
            using var ins = conn.CreateCommand();
            ins.CommandText = @"INSERT OR IGNORE INTO parcels(id, parcelNumber, ownerName, siteAddress, landValue, improvedValue, latitude, longitude)
                                VALUES ($id,$pn,$on,$addr,$lv,$iv,$lat,$lng)";
            ins.Parameters.AddWithValue("$id", p.id);
            ins.Parameters.AddWithValue("$pn", p.parcelNumber);
            ins.Parameters.AddWithValue("$on", p.ownerName);
            ins.Parameters.AddWithValue("$addr", p.siteAddress);
            ins.Parameters.AddWithValue("$lv", p.landValue);
            ins.Parameters.AddWithValue("$iv", p.improvedValue);
            ins.Parameters.AddWithValue("$lat", p.latitude);
            ins.Parameters.AddWithValue("$lng", p.longitude);
            ins.ExecuteNonQuery();
        }
    }
});

app.MapGet("/parcels", (string? q) =>
{
    using var conn = new SqliteConnection(connStr);
    conn.Open();
    var where = string.IsNullOrWhiteSpace(q) ? "" : "WHERE parcelNumber LIKE $q OR ownerName LIKE $q OR siteAddress LIKE $q";
    using var cmd = conn.CreateCommand();
    cmd.CommandText = $@"SELECT id, parcelNumber, ownerName, siteAddress, landValue, improvedValue, totalValue, latitude, longitude
                         FROM parcels {where}
                         ORDER BY totalValue DESC
                         LIMIT 50";
    if (!string.IsNullOrWhiteSpace(q)) cmd.Parameters.AddWithValue("$q", $"%{q}%");
    using var rdr = cmd.ExecuteReader();
    var list = new List<Parcel>();
    while (rdr.Read())
    {
        list.Add(new Parcel{
            id = rdr.GetString(0), parcelNumber = rdr.GetString(1), ownerName = rdr.GetString(2),
            siteAddress = rdr.GetString(3), landValue = rdr.GetDouble(4), improvedValue = rdr.GetDouble(5),
            totalValue = rdr.GetDouble(6), latitude = rdr.GetDouble(7), longitude = rdr.GetDouble(8)
        });
    }
    return Results.Ok(list);
});

app.MapGet("/parcel/{id}", (string id) =>
{
    using var conn = new SqliteConnection(connStr);
    conn.Open();
    using var cmd = conn.CreateCommand();
    cmd.CommandText = @"SELECT id, parcelNumber, ownerName, siteAddress, landValue, improvedValue, totalValue, latitude, longitude
                        FROM parcels WHERE id=$id";
    cmd.Parameters.AddWithValue("$id", id);
    using var rdr = cmd.ExecuteReader();
    if (!rdr.Read()) return Results.NotFound();
    var p = new Parcel{
      id = rdr.GetString(0), parcelNumber = rdr.GetString(1), ownerName = rdr.GetString(2),
      siteAddress = rdr.GetString(3), landValue = rdr.GetDouble(4), improvedValue = rdr.GetDouble(5),
      totalValue = rdr.GetDouble(6), latitude = rdr.GetDouble(7), longitude = rdr.GetDouble(8)
    };
    return Results.Ok(p);
});

// Live passthrough to ArcGIS FeatureServer for quick demos
app.MapGet("/parcels/live", async (string? q) =>
{
    string where = "1=1";
    if (!string.IsNullOrWhiteSpace(q))
    {
        var safe = q.Replace("'", "''");
        if (System.Text.RegularExpressions.Regex.IsMatch(safe, @"^\d"))
            where = $"FOLIO = '{safe}'";
        else
            where = $"UPPER(OWNER) LIKE UPPER('%{safe}%') OR UPPER(SITE_ADDR) LIKE UPPER('%{safe}%')";
    }

    var form = new Dictionary<string,string> {
        { "f", "json" },
        { "where", where },
        { "outFields", "FOLIO,OWNER,SITE_ADDR,JUST,LAND,BLDG" },
        { "returnGeometry", "false" },
        { "resultRecordCount", "25" }
    };

    var svc = "https://arcgis.tampagov.net/arcgis/rest/services/Parcels/TaxParcel/FeatureServer/0/query";
    using var http = new HttpClient();
    using var res = await http.PostAsync(svc, new FormUrlEncodedContent(form));
    res.EnsureSuccessStatusCode();

    var doc = await res.Content.ReadFromJsonAsync<JsonElement>();
    if (doc.TryGetProperty("error", out var err)) return Results.Problem(err.ToString());

    var list = new List<object>();
    foreach (var f in doc.GetProperty("features").EnumerateArray())
    {
        var a = f.GetProperty("attributes");
        double getDouble(JsonElement e, string name)
        {
            var v = a.GetProperty(name);
            if (v.ValueKind == JsonValueKind.Number && v.TryGetDouble(out var d)) return d;
            return 0;
        }
        list.Add(new {
            id = a.GetProperty("FOLIO").ToString(),
            parcelNumber = a.GetProperty("FOLIO").ToString(),
            ownerName = a.GetProperty("OWNER").ToString(),
            siteAddress = a.GetProperty("SITE_ADDR").ToString(),
            landValue = getDouble(a, "LAND"),
            improvedValue = getDouble(a, "BLDG"),
            totalValue = getDouble(a, "JUST")
        });
    }
    return Results.Ok(list);
});

app.Run();

record Parcel
{
  public string id { get; set; } = Guid.NewGuid().ToString();
  public string parcelNumber { get; set; } = "";
  public string ownerName { get; set; } = "";
  public string siteAddress { get; set; } = "";
  public double landValue { get; set; }
  public double improvedValue { get; set; }
  public double totalValue { get; set; }
  public double latitude { get; set; }
  public double longitude { get; set; }
}
