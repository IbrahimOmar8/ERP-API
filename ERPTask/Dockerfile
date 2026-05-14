# ─── build stage ─────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files first for layer caching of `dotnet restore`
COPY ERPTask.sln ./
COPY Domain/Domain.csproj          Domain/
COPY Application/Application.csproj Application/
COPY Infrastructure/Infrastructure.csproj Infrastructure/
COPY ERPTask/ERPTask.csproj        ERPTask/
RUN dotnet restore ERPTask.sln

# Copy the rest of the source and publish
COPY Domain          Domain/
COPY Application     Application/
COPY Infrastructure  Infrastructure/
COPY ERPTask         ERPTask/
RUN dotnet publish ERPTask/ERPTask.csproj -c Release -o /app /p:UseAppHost=false

# ─── runtime stage ───────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app ./

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "ERPTask.dll"]
