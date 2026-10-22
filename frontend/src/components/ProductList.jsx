import React from 'react';
import ProductCard from './ProductCard';
export default function ProductList({ products, ...actions }) { return <div className="product-grid">{products.map(p => <ProductCard key={p.id + p.foto} product={p} {...actions}/>)}</div>; }
