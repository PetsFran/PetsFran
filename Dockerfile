FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Instalar ferramenta EF Core globalmente no build
RUN dotnet tool install --global dotnet-ef
ENV PATH="${PATH}:/root/.dotnet/tools"

# Copiar arquivos de projeto
COPY *.sln .
COPY PetMind.API/*.csproj ./PetMind.API/
RUN dotnet restore

# Copiar todo o código e publicar
COPY . .
WORKDIR /app/PetMind.API
RUN dotnet publish -c Release -o /out

# Imagem de runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Copiar a ferramenta EF Core da imagem de build
COPY --from=build /root/.dotnet/tools /root/.dotnet/tools
ENV PATH="${PATH}:/root/.dotnet/tools"

# Copiar os arquivos publicados
COPY --from=build /out .

# Script de entrada
COPY entrypoint.sh .
RUN chmod +x entrypoint.sh

CMD ./entrypoint.sh