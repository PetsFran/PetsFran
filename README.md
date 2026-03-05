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
   ```bash
   git clone https://github.com/PetsFran/PetsFran.API.git
   cd PetsFran.API
   ```

2. **Configure o banco de dados**
   - Altere a connection string no `appsettings.json`
   - Ou configure a variável de ambiente `DATABASE_URL`

3. **Execute as migrações**
   ```bash
   dotnet ef database update
   ```

4. **Execute a aplicação**
   ```bash
   dotnet run
   ```

5. **Acesse a documentação**
   - Swagger: http://localhost:5028/swagger
   - Scalar: http://localhost:5028/scalar/v1

## 🐳 Docker

```bash
# Build da imagem
docker build -t petsfran-api .

# Executar container
docker run -p 5028:5028 petsfran-api
```

## 🌐 Deploy no Railway

A API está configurada para deploy no Railway com:
- Dockerfile otimizado
- Migrações automáticas
- Variáveis de ambiente para produção

## 📁 Estrutura do Projeto

```
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
```

## 📝 Licença
Este projeto é privado e de uso exclusivo da PetsFran.
