import { useEffect, useState } from 'react'
import { searchParcelsLive } from './api'

type Parcel = {
  id: string; parcelNumber: string; ownerName: string; siteAddress: string;
  landValue: number; improvedValue: number; totalValue: number
}

export default function App() {
  const [q, setQ] = useState('SMITH')
  const [rows, setRows] = useState<Parcel[]>([])
  const [sel, setSel] = useState<Parcel | null>(null)

  useEffect(() => { (async () => setRows(await searchParcelsLive(q)))() }, [])

  return (
    <div style={{fontFamily:'Inter, system-ui', padding:16, maxWidth:1100, margin:'0 auto'}}>
      <h1>Property Appraiser – Parcel Search (POC)</h1>
      <div style={{display:'flex', gap:8, marginBottom:12}}>
        <input value={q} onChange={e=>setQ(e.target.value)} placeholder="Owner, parcel #, address" style={{flex:1, padding:8}} />
        <button onClick={async()=> setRows(await searchParcelsLive(q))}>Search (Live)</button>
      </div>
      <div style={{display:'grid', gridTemplateColumns:'2fr 1fr', gap:16}}>
        <div style={{overflowX:'auto'}}>
          <table width="100%" cellPadding={6} style={{borderCollapse:'collapse'}}>
            <thead>
              <tr><th align="left">Parcel #</th><th align="left">Owner</th><th align="left">Address</th><th align="right">Total</th></tr>
            </thead>
            <tbody>
              {rows.map(r=> (
                <tr key={r.id} style={{cursor:'pointer'}} onClick={()=> setSel(r)}>
                  <td>{r.parcelNumber}</td>
                  <td>{r.ownerName}</td>
                  <td>{r.siteAddress}</td>
                  <td align="right">${(r.totalValue ?? 0).toLocaleString()}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <div>
          {sel ? (
            <div style={{border:'1px solid #ddd', borderRadius:8, padding:12}}>
              <h3 style={{marginTop:0}}>Parcel Detail</h3>
              <div><b>Parcel #:</b> {sel.parcelNumber}</div>
              <div><b>Owner:</b> {sel.ownerName}</div>
              <div><b>Site:</b> {sel.siteAddress}</div>
              <div><b>Land:</b> ${(sel.landValue ?? 0).toLocaleString()}</div>
              <div><b>Impr:</b> ${(sel.improvedValue ?? 0).toLocaleString()}</div>
              <div><b>Total:</b> ${(sel.totalValue ?? 0).toLocaleString()}</div>
            </div>
          ) : <div style={{opacity:.6}}>Select a row</div>}
        </div>
      </div>
    </div>
  )
}
