#!/bin/bash

echo "=== Iniciando PetMind API ==="
echo "Aplicando migrações do banco de dados..."
dotnet ef database update --no-build

echo "Iniciando a aplicação..."
dotnet PetMind.API.dll