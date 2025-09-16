const SVC = "https://arcgis.tampagov.net/arcgis/rest/services/Parcels/TaxParcel/FeatureServer/0/query";

export async function searchParcelsLive(q: string) {
  // Build ArcGIS 'where'
  let where = "1=1";
  if (q?.trim()) {
    const safe = q.replace(/'/g, "''");
    if (/^\d/.test(safe)) where = `FOLIO='${safe}'`;
    else where = `UPPER(OWNER) LIKE UPPER('%${safe}%') OR UPPER(SITE_ADDR) LIKE UPPER('%${safe}%')`;
  }

  const form = new URLSearchParams();
  form.set("f","json");
  form.set("where", where);
  form.set("outFields","FOLIO,OWNER,SITE_ADDR,JUST,LAND,BLDG");
  form.set("returnGeometry","false");
  form.set("resultRecordCount","25");

  const res = await fetch(SVC, {
    method: "POST",
    headers: { "Content-Type": "application/x-www-form-urlencoded" },
    body: form.toString()
  });

  const json = await res.json();
  if (json.error) throw new Error(JSON.stringify(json.error));

  return json.features.map((f: any) => {
    const a = f.attributes;
    return {
      id: a.FOLIO?.toString(),
      parcelNumber: a.FOLIO?.toString(),
      ownerName: a.OWNER,
      siteAddress: a.SITE_ADDR,
      landValue: Number(a.LAND || 0),
      improvedValue: Number(a.BLDG || 0),
      totalValue: Number(a.JUST || 0),
    };
  });
}
