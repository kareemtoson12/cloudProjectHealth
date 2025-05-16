# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy and restore project files
COPY ["PatientManagement.API/PatientManagement.API.csproj", "PatientManagement.API/"]
COPY ["HealthcareManagement.Shared/HealthcareManagement.Shared.csproj", "HealthcareManagement.Shared/"]
RUN dotnet restore "PatientManagement.API/PatientManagement.API.csproj"

# Copy and build source code
COPY . .
WORKDIR "/src/PatientManagement.API"
RUN dotnet build "PatientManagement.API.csproj" -c Release -o /app/build
RUN dotnet publish "PatientManagement.API.csproj" -c Release -o /app/publish

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copy published files
COPY --from=build /app/publish .

# Set environment variables
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 80
ENTRYPOINT ["dotnet", "PatientManagement.API.dll"] 