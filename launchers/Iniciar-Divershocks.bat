@echo off
setlocal
cd /d "%~dp0"
start "Divershocks" http://localhost:5050
echo Divershocks esta funcionando en http://localhost:5050
echo No cierres esta ventana mientras uses la aplicacion.
DivershocksApi.exe --urls http://0.0.0.0:5050
