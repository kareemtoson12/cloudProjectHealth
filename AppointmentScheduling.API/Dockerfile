# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy and restore project files
COPY ["AppointmentScheduling.API/AppointmentScheduling.API.csproj", "AppointmentScheduling.API/"]
COPY ["HealthcareManagement.Shared/HealthcareManagement.Shared.csproj", "HealthcareManagement.Shared/"]
RUN dotnet restore "AppointmentScheduling.API/AppointmentScheduling.API.csproj"

# Copy and build source code
COPY . .
WORKDIR "/src/AppointmentScheduling.API"
RUN dotnet build "AppointmentScheduling.API.csproj" -c Release -o /app/build
RUN dotnet publish "AppointmentScheduling.API.csproj" -c Release -o /app/publish

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copy published files
COPY --from=build /app/publish .

# Set environment variables
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV ASPNETCORE_URLS=http://+:80

EXPOSE 80
ENTRYPOINT ["dotnet", "AppointmentScheduling.API.dll"] 