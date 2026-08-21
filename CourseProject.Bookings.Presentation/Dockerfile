# ── Stage 1: Базовый образ ──
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY CourseProject.Bookings.Presentation/CourseProject.Bookings.Presentation.csproj CourseProject.Bookings.Presentation/
COPY CourseProject.Bookings.Infrastructure/CourseProject.Bookings.Infrastructure.csproj CourseProject.Bookings.Infrastructure/
COPY CourseProject.Bookings.Application/CourseProject.Bookings.Application.csproj CourseProject.Bookings.Application/
COPY CourseProject.Bookings.Domain/CourseProject.Bookings.Domain.csproj CourseProject.Bookings.Domain/
COPY CourseProject.Contracts/CourseProject.Contracts.csproj CourseProject.Contracts/

RUN dotnet restore CourseProject.Bookings.Presentation/CourseProject.Bookings.Presentation.csproj

COPY . .
WORKDIR /src/CourseProject.Bookings.Presentation
RUN dotnet publish CourseProject.Bookings.Presentation.csproj -c Release -o /app/publish /p:UseAppHost=false --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

RUN if ! id -u app >/dev/null 2>&1; then adduser -D app; fi
USER app

ENTRYPOINT ["dotnet", "CourseProject.Bookings.Presentation.dll"]