# ============ Stage 1: Build Frontend ============
FROM node:23-alpine AS frontend-build
WORKDIR /src/frontend
COPY frontend/package.json frontend/package-lock.json* ./
RUN npm install
COPY frontend/ ./
RUN npm run build

# ============ Stage 2: Build Backend ============
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS backend-build
WORKDIR /src/backend
COPY backend/*.csproj .
RUN dotnet restore
COPY backend/ ./
RUN dotnet publish -c Release -o /app/publish

# ============ Stage 3: Runtime ============
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine
WORKDIR /app

# Copy backend
COPY --from=backend-build /app/publish .

# Copy frontend dist
COPY --from=frontend-build /src/frontend/dist ./wwwroot

# Create data directories
RUN mkdir -p /data/projects

ENV DOTNET_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://0.0.0.0:5100

EXPOSE 5100

ENTRYPOINT ["dotnet", "DotNetHub.Server.dll"]
