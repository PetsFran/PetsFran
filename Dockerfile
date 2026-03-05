# Estágio de build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copiar arquivos de projeto e restaurar dependências
COPY *.sln .
COPY PetsFran.API/*.csproj ./PetsFran.API/
RUN dotnet restore

# Instalar ferramenta do EF Core
RUN dotnet tool install --tool-path /app/tools dotnet-ef --version 10.0.0

# Copiar todo o código e publicar a aplicação
COPY . .
WORKDIR /app/PetsFran.API
RUN dotnet publish -c Release -o /out

# Estágio final (runtime)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Copiar ferramentas e aplicação
COPY --from=build /app/tools /app/tools
COPY --from=build /out .

# Copiar script de entrada
COPY entrypoint.sh .
RUN chmod +x entrypoint.sh

# Expor portas
EXPOSE 8080
EXPOSE 8081

# Comando de entrada
CMD ["./entrypoint.sh"]