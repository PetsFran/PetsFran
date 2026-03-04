# Na imagem de runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# 🔥 Instalar o SDK e as ferramentas do EF Core
RUN apt-get update && \
    apt-get install -y wget && \
    wget https://dot.net/v1/dotnet-install.sh && \
    chmod +x dotnet-install.sh && \
    ./dotnet-install.sh --channel 10.0 --install-dir /usr/share/dotnet && \
    ln -s /usr/share/dotnet/dotnet /usr/bin/dotnet

ENV PATH="${PATH}:/root/.dotnet/tools"
RUN dotnet tool install --global dotnet-ef --version 10.0.0

# Copiar os arquivos publicados
COPY --from=build /out .
COPY entrypoint.sh .
RUN chmod +x entrypoint.sh

CMD ./entrypoint.sh