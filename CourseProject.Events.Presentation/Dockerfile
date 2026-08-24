# ── Stage 1: Базовый образ ──
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# ── Stage 2: Восстановление и публикация ──
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Копируем csproj всех проектов, включая Contracts
COPY CourseProject.Events.Presentation/CourseProject.Events.Presentation.csproj CourseProject.Events.Presentation/
COPY CourseProject.Events.Infrastructure/CourseProject.Events.Infrastructure.csproj CourseProject.Events.Infrastructure/
COPY CourseProject.Events.Application/CourseProject.Events.Application.csproj CourseProject.Events.Application/
COPY CourseProject.Events.Domain/CourseProject.Events.Domain.csproj CourseProject.Events.Domain/
COPY CourseProject.Contracts/CourseProject.Contracts.csproj CourseProject.Contracts/

# Восстанавливаем зависимости
RUN dotnet restore CourseProject.Events.Presentation/CourseProject.Events.Presentation.csproj

# Копируем весь оставшийся исходный код
COPY . .
WORKDIR /src/CourseProject.Events.Presentation
RUN dotnet publish CourseProject.Events.Presentation.csproj -c Release -o /app/publish /p:UseAppHost=false --no-restore

# ── Stage 3: Финальный образ ──
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

RUN if ! id -u app >/dev/null 2>&1; then adduser -D app; fi
USER app

ENTRYPOINT ["dotnet", "CourseProject.Events.Presentation.dll"]