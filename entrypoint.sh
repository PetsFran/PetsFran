#!/bin/bash

echo "=== Iniciando PetMind API ==="
echo "Aplicando migrações do banco de dados..."

# Vai para o diretório da aplicação
cd /app

# Tenta aplicar as migrações usando o caminho completo da ferramenta
/app/tools/dotnet-ef database update --no-build

# Verifica se as migrações funcionaram (opcional)
if [ $? -eq 0 ]; then
    echo "Migrações aplicadas com sucesso!"
else
    echo "Falha ao aplicar migrações, mas continuando com a inicialização..."
fi

echo "Iniciando a aplicação..."
dotnet PetMind.API.dll