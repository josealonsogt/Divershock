const base = import.meta.env.PROD ? '' : (import.meta.env.VITE_API_URL || '');
async function request(path, options = {}) {
  let response;
  try { response = await fetch(base + path, { ...options, headers: { 'Content-Type': 'application/json', ...options.headers } }); }
  catch { throw new Error('No se puede conectar con la tienda. Comprueba que el servidor esté encendido.'); }
  const text = await response.text();
  let data; try { data = JSON.parse(text); } catch { data = text; }
  if (!response.ok) throw new Error(data?.detail || data?.title || data || 'No se pudo completar la operación.');
  return data;
}
export const api = {
  products: () => request('/productos'),
  save: (data, id) => request('/productos' + (id ? '/' + id : ''), { method: id ? 'PUT' : 'POST', body: JSON.stringify(data) }),
  size: (id, talla) => request(`/productos/${id}/variantes?talla=${encodeURIComponent(talla)}`, { method: 'POST' }),
  move: data => request('/movimientos', { method: 'POST', body: JSON.stringify(data) }),
  history: id => request('/movimientos/' + id),
  recentMovements: (limit = 12) => request('/movimientos?limite=' + limit),
  deleteMovement: id => request('/movimientos/' + id, { method: 'DELETE' }),
  clearHistory: variantId => request('/movimientos/variante/' + variantId, { method: 'DELETE' }),
  clearAllHistory: () => request('/movimientos', { method: 'DELETE' }),
  deleteProduct: id => request('/productos/' + id, { method: 'DELETE' }),
};
