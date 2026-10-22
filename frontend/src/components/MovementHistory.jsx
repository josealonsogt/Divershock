import React, { useEffect, useState } from 'react';
import Modal from './Modal';
import { api } from '../api';
import { types } from './MovementModal';

export default function MovementHistory({ product, variant, onClose }) {
 const [rows, setRows] = useState(null); const [error, setError] = useState(''); const [retry, setRetry] = useState(0);
 useEffect(() => { let active = true; setError(''); api.history(variant.varianteId).then(r => active && setRows(r)).catch(e => active && setError(e.message)); return () => { active = false; }; }, [variant.varianteId, retry]);
 return <Modal title="Historial de movimientos" onClose={onClose}><p className="context-label">{product.nombre} · Talla {variant.talla}</p><p className="muted">Consulta aquí los movimientos de esta talla. Para vaciar pruebas usa la pestaña Últimos movimientos.</p>{error ? <p role="alert" className="error">{error} <button onClick={() => setRetry(retry + 1)}>Reintentar</button></p> : !rows ? <p role="status">Cargando historial…</p> : !rows.length ? <div className="empty-small">Esta talla todavía no tiene movimientos.</div> : <ul className="history-list">{rows.map(r => <li key={r.id}><div><strong>{types[r.tipo] || r.tipo}</strong><time>{new Date(r.fecha.replace(' ', 'T')).toLocaleString('es-ES')}</time></div><b>{r.cantidad} <small>pares</small></b></li>)}</ul>}</Modal>;
}
