#!/bin/bash

echo "=== Iniciando PetMind API ==="
echo "Aplicando migrações do banco de dados..."

# Tenta aplicar as migrações. Se falhar, loga o erro mas continua para não travar o container.
dotnet ef database update --no-build

echo "Iniciando a aplicação..."
dotnet PetMind.API.dll