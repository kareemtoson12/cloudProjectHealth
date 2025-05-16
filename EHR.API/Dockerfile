# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy and restore project files
COPY ["EHR.API/EHR.API.csproj", "EHR.API/"]
COPY ["HealthcareManagement.Shared/HealthcareManagement.Shared.csproj", "HealthcareManagement.Shared/"]
RUN dotnet restore "EHR.API/EHR.API.csproj"

# Copy and build source code
COPY . .
WORKDIR "/src/EHR.API"
RUN dotnet build "EHR.API.csproj" -c Release -o /app/build
RUN dotnet publish "EHR.API.csproj" -c Release -o /app/publish

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copy published files
COPY --from=build /app/publish .

# Set environment variables
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV ASPNETCORE_URLS=http://+:80

EXPOSE 80
ENTRYPOINT ["dotnet", "EHR.API.dll"] 