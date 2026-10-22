import React, { useEffect, useRef } from 'react';
export default function Modal({ title, onClose, children }) {
  const ref = useRef();
  useEffect(() => { const dialog = ref.current; dialog.showModal(); return () => dialog.close(); }, []);
  return <dialog ref={ref} onCancel={e => { e.preventDefault(); onClose(); }} aria-labelledby="modal-title"><div className="modal-head"><h2 id="modal-title">{title}</h2><button className="icon-button" onClick={onClose} aria-label="Cerrar">×</button></div>{children}</dialog>;
}
