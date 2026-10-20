FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY PetsFran.API/PetsFran.API.csproj PetsFran.API/
RUN dotnet restore PetsFran.API/PetsFran.API.csproj
COPY PetsFran.API/ PetsFran.API/
RUN dotnet publish PetsFran.API/PetsFran.API.csproj -c Release -o /out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out .
EXPOSE 8080
ENTRYPOINT ["dotnet", "PetsFran.API.dll"]
