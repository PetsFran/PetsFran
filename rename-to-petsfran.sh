#!/bin/bash

echo "🔧 Renomeando projeto ASP.NET de PetsFran para PetsFran.API..."

# Configurações
OLD_NAME="PetsFran"
NEW_NAME="PetsFran"
NEW_NAME_WITH_API="PetsFran.API"

# 1. Renomear pasta principal
if [ -d "$OLD_NAME.API" ]; then
    mv "$OLD_NAME.API" "$NEW_NAME.API"
    echo "✅ Pasta renomeada: $OLD_NAME.API -> $NEW_NAME.API"
fi

# 2. Renomear arquivo .sln
if [ -f "$OLD_NAME.sln" ]; then
    mv "$OLD_NAME.sln" "$NEW_NAME.sln"
    echo "✅ Solução renomeada: $OLD_NAME.sln -> $NEW_NAME.sln"
fi

# 3. Renomear arquivo .csproj
if [ -f "$OLD_NAME.API/$OLD_NAME.API.csproj" ]; then
    mv "$OLD_NAME.API/$OLD_NAME.API.csproj" "$OLD_NAME.API/$NEW_NAME.API.csproj"
    echo "✅ .csproj renomeado: $OLD_NAME.API.csproj -> $NEW_NAME.API.csproj"
fi

# 4. Renomear arquivo .http (se existir)
if [ -f "$OLD_NAME.API/$OLD_NAME.API.http" ]; then
    mv "$OLD_NAME.API/$OLD_NAME.API.http" "$OLD_NAME.API/$NEW_NAME.API.http"
    echo "✅ .http renomeado: $OLD_NAME.API.http -> $NEW_NAME.API.http"
fi

# 5. Substituir conteúdo em todos os arquivos
echo "📄 Atualizando conteúdo dos arquivos..."

# Arquivos de configuração e código
find . -type f \( -name "*.cs" -o -name "*.json" -o -name "*.csproj" -o -name "*.sln" -o -name "*.http" -o -name "Dockerfile" -o -name "*.sh" \) | while read file; do
    if [ -f "$file" ]; then
        # Substituir PetsFran por PetsFran (mas preservar PetsFran.API quando necessário)
        sed -i '' "s/$OLD_NAME/$NEW_NAME/g" "$file"
        sed -i '' "s/PetsFran\.API/PetsFran.API/g" "$file"
        echo "  📄 Atualizado: $file"
    fi
done

# 6. Atualizar Program.cs especificamente (caso precise de ajustes finos)
if [ -f "$NEW_NAME.API/Program.cs" ]; then
    # Garantir que os usings estão corretos
    sed -i '' "s/using $OLD_NAME/using $NEW_NAME/g" "$NEW_NAME.API/Program.cs"
    echo "✅ Program.cs atualizado"
fi

# 7. Atualizar Dockerfile
if [ -f "Dockerfile" ]; then
    sed -i '' "s/$OLD_NAME/$NEW_NAME/g" Dockerfile
    echo "✅ Dockerfile atualizado"
fi

# 8. Atualizar entrypoint.sh
if [ -f "entrypoint.sh" ]; then
    sed -i '' "s/$OLD_NAME/$NEW_NAME/g" entrypoint.sh
    echo "✅ entrypoint.sh atualizado"
fi

# 9. Atualizar .gitignore (se necessário)
if [ -f ".gitignore" ]; then
    sed -i '' "s/$OLD_NAME/$NEW_NAME/g" .gitignore
    echo "✅ .gitignore atualizado"
fi

# 10. Criar novo README.md
cat > README.md << EOF
# PetsFran.API

API do sistema de agendamento PetsFran.

## 🚀 Tecnologias
- ASP.NET Core 10
- Entity Framework Core
- PostgreSQL
- JWT Authentication
- Swagger/OpenAPI

## 📋 Pré-requisitos
- .NET 10 SDK
- PostgreSQL
- Docker (opcional)

## 🔧 Configuração

1. **Clone o repositório**
   \`\`\`bash
   git clone https://github.com/PetsFran/PetsFran.API.git
   cd PetsFran.API
   \`\`\`

2. **Configure o banco de dados**
   - Altere a connection string no \`appsettings.json\`
   - Ou configure a variável de ambiente \`DATABASE_URL\`

3. **Execute as migrações**
   \`\`\`bash
   dotnet ef database update
   \`\`\`

4. **Execute a aplicação**
   \`\`\`bash
   dotnet run
   \`\`\`

5. **Acesse a documentação**
   - Swagger: http://localhost:5028/swagger
   - Scalar: http://localhost:5028/scalar/v1

## 🐳 Docker

\`\`\`bash
# Build da imagem
docker build -t petsfran-api .

# Executar container
docker run -p 5028:5028 petsfran-api
\`\`\`

## 🌐 Deploy no Railway

A API está configurada para deploy no Railway com:
- Dockerfile otimizado
- Migrações automáticas
- Variáveis de ambiente para produção

## 📁 Estrutura do Projeto

\`\`\`
PetsFran.API/
├── Controllers/     # Controladores da API
├── Data/            # Contexto do banco de dados
├── Middleware/      # Middlewares personalizados
├── Models/          # Modelos/Entidades
├── Profiles/        # Perfis do AutoMapper
├── Properties/      # Configurações do launchSettings.json
├── Services/        # Lógica de negócios
├── appsettings.json # Configurações
└── Program.cs       # Ponto de entrada
\`\`\`

## 📝 Licença
Este projeto é privado e de uso exclusivo da PetsFran.
EOF
echo "✅ README.md atualizado"

echo ""
echo "🎉 Projeto renomeado com sucesso para PetsFran.API!"
echo ""
echo "📌 Resumo das alterações:"
echo "   - Pasta: $OLD_NAME.API → $NEW_NAME.API"
echo "   - Solução: $OLD_NAME.sln → $NEW_NAME.sln"
echo "   - Projeto: $OLD_NAME.API.csproj → $NEW_NAME.API.csproj"
echo "   - Namespaces: $OLD_NAME → $NEW_NAME em todos os arquivos"
echo ""
echo "📋 Próximos passos:"
echo "1. Verifique as alterações: git status"
echo "2. Veja as diferenças: git diff"
echo "3. Compile o projeto: dotnet build"
echo "4. Teste localmente: dotnet run --project $NEW_NAME.API/$NEW_NAME.API.csproj"
echo "5. Commit: git add . && git commit -m \"rename: PetsFran para PetsFran.API\""
echo "6. Atualize o remote: git remote set-url origin https://github.com/PetsFran/PetsFran.API.git"
echo "7. Push: git push -u origin main"