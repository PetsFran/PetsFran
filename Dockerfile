# Estágio de build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copiar arquivos de projeto e restaurar dependências
COPY *.sln .
COPY PetMind.API/*.csproj ./PetMind.API/
RUN dotnet restore

# Instalar a ferramenta do EF Core globalmente e salvar em uma pasta local
RUN dotnet tool install --global dotnet-ef --version 10.0.0

# Copiar todo o código e publicar a aplicação
COPY . .
WORKDIR /app/PetMind.API
RUN dotnet publish -c Release -o /out

# Estágio final (runtime)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Copiar a ferramenta do EF Core do estágio de build
COPY --from=build /root/.dotnet/tools /root/.dotnet/tools

# Atualizar a variável de ambiente PATH para encontrar a ferramenta
ENV PATH="${PATH}:/root/.dotnet/tools"

# Copiar a aplicação publicada do estágio de build
COPY --from=build /out .

# Copiar e dar permissão ao script de entrada
COPY entrypoint.sh .
RUN chmod +x entrypoint.sh

# Comando de entrada
CMD ["./entrypoint.sh"]