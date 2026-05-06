FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
  WORKDIR /src

  COPY backend.slnx ./
  COPY API/API.csproj API/
  COPY Application/Application.csproj Application/
  COPY Domain/Domain.csproj Domain/
  COPY Persistence/Persistence.csproj Persistence/

  RUN dotnet restore API/API.csproj

  COPY . .
  RUN dotnet publish API/API.csproj -c Release -o /app/publish

  FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
  WORKDIR /app

  COPY --from=build /app/publish .

  EXPOSE 8080

  ENTRYPOINT ["dotnet", "API.dll"]