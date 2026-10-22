#!/bin/zsh
cd "$(dirname "$0")"
open http://localhost:5050
echo "Divershocks esta funcionando en http://localhost:5050"
echo "No cierres esta ventana mientras uses la aplicacion."
./DivershocksApi --urls http://0.0.0.0:5050
