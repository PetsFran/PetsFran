#!/bin/bash

echo "=== Iniciando PetMind API ==="
echo "Aplicando migrações do banco de dados..."

# Usa o caminho completo para a ferramenta instalada localmente
/app/tools/dotnet-ef database update --no-build

echo "Iniciando a aplicação..."
dotnet PetMind.API.dll