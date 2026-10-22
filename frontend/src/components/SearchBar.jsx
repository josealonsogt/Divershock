import React from 'react';
export default function SearchBar({ search, setSearch, category, setCategory, categories }) {
  return <div className="searchbar"><label className="search"><span aria-hidden="true">⌕</span><input aria-label="Buscar por nombre o referencia" placeholder="Busca tus calcetines por nombre o referencia…" value={search} onChange={e => setSearch(e.target.value)}/></label><select aria-label="Filtrar por categoría" value={category} onChange={e => setCategory(e.target.value)}><option value="">Todas las categorías</option>{categories.map(c => <option key={c}>{c}</option>)}</select></div>;
}
