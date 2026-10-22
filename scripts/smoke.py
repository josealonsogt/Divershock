"""Integration checks against an isolated copy of SQLite; never edits shop data."""
import concurrent.futures, json, os, pathlib, shutil, subprocess, tempfile, time, urllib.request, urllib.error
root = pathlib.Path(__file__).resolve().parents[1]
with tempfile.TemporaryDirectory(prefix='divershocks-test-') as tmp:
    db = pathlib.Path(tmp) / 'test.db'
    shutil.copy2(root / 'diverShocks.db', db)
    env = dict(os.environ, ConnectionStrings__DefaultConnection=f'Data Source={db}', ASPNETCORE_URLS='http://127.0.0.1:5099', ASPNETCORE_ENVIRONMENT='Production')
    with open(pathlib.Path(tmp) / 'server.log', 'w+') as log:
        server = subprocess.Popen(['dotnet', str(root / 'bin/Debug/net10.0/DivershocksApi.dll')], cwd=root, env=env, stdout=log, stderr=log)
        def call(path, data=None, method=None):
            req = urllib.request.Request('http://127.0.0.1:5099' + path, data=json.dumps(data).encode() if data is not None else None, method=method, headers={'Content-Type':'application/json'})
            try:
                with urllib.request.urlopen(req) as response: return response.status, response.read().decode()
            except urllib.error.HTTPError as e: return e.code, e.read().decode()
        try:
            for _ in range(100):
                try: call('/productos'); break
                except OSError: time.sleep(.1)
            else:
                log.seek(0); raise RuntimeError(log.read())
            assert call('/')[0] == 200
            product = dict(referencia='SMOKE-TEST', nombre='Prueba', categoria='Test', foto='', precio=12.50, tallas=['36-40','40-46'])
            assert call('/productos',product)[0] == 200
            assert call('/productos',product)[0] == 400
            bad=dict(product,referencia='BAD',tallas=['36-40','36-40'])
            assert call('/productos',bad)[0] == 400
            p=next(p for p in json.loads(call('/productos')[1]) if p['referencia']=='SMOKE-TEST')
            vid=p['tallas'][0]['varianteId']
            def move(tipo,n): return call('/movimientos',dict(varianteId=vid,tipo=tipo,cantidad=n))[0]
            assert move('Venta',1)==400
            assert move('EntradaAlmacen',10)==200
            assert move('TrasladoTienda',11)==400
            assert move('TrasladoTienda',6)==200
            assert move('AjusteStock',-5)==400
            assert move('AjusteStock',-2)==200
            with concurrent.futures.ThreadPoolExecutor(2) as pool:
                codes=list(pool.map(lambda _: move('Venta',4),range(2)))
            assert sorted(codes)==[200,400], codes
            p=next(p for p in json.loads(call('/productos')[1]) if p['id']==p['id'] and p['referencia']=='SMOKE-TEST')
            v=next(v for v in p['tallas'] if v['varianteId']==vid)
            assert (v['stockAlmacen'],v['stockTienda'])==(2,2),v
            assert len(json.loads(call(f'/movimientos/{vid}')[1]))==4
            assert call(f'/productos/{p["id"]}/variantes?talla=36-40',method='POST')[0]==400
            assert call(f'/productos/{p["id"]}/variantes?talla=30-35',method='POST')[0]==200
            assert call('/movimientos/2147483647')[0]==404
            assert list(pathlib.Path(tmp).glob('*.bak'))
            print('PASS: static web, migration + backup, products, duplicate validation, sizes, stock, signed adjustment, history, concurrent sales and 404.')
        finally:
            server.terminate(); server.wait(timeout=10)
