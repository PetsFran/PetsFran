#!/bin/bash

echo "=== Iniciando PetMind API ==="
echo "Aplicando migrações do banco de dados..."

# Vai para o diretório da aplicação
cd /app

# 🔥 Usa a DLL da aplicação como contexto para as migrações
/app/tools/dotnet-ef database update --no-build --context AppDbContext

# Verifica se as migrações funcionaram
if [ $? -eq 0 ]; then
    echo "Migrações aplicadas com sucesso!"
else
    echo "Falha ao aplicar migrações, mas continuando com a inicialização..."
    echo "Nota: As tabelas podem não existir no banco de dados."
fi

echo "Iniciando a aplicação..."
dotnet PetMind.API.dll