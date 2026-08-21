# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# ── Stage 1: Базовый образ для работы приложения ──
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Устанавливаем ICU-библиотеки для корректной работы локализации и дат в Alpine
RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# ── Stage 2: Восстановление пакетов и компиляция проекта ──
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Копируем проекты Чистой Архитектуры для кэширования Nuget-пакетов
COPY CourseProject.Users.Presentation/CourseProject.Users.Presentation.csproj CourseProject.Users.Presentation/
COPY CourseProject.Users.Infrastructure/CourseProject.Users.Infrastructure.csproj CourseProject.Users.Infrastructure/
COPY CourseProject.Users.Application/CourseProject.Users.Application.csproj CourseProject.Users.Application/
COPY CourseProject.Users.Domain/CourseProject.Users.Domain.csproj CourseProject.Users.Domain/

# Восстанавливаем зависимости с правильным синтаксисом путей
RUN dotnet restore CourseProject.Users.Presentation/CourseProject.Users.Presentation.csproj

# Копируем весь оставшийся исходный код
COPY . .
WORKDIR /src/CourseProject.Users.Presentation

# Собираем проект
RUN dotnet publish CourseProject.Users.Presentation.csproj -c Release -o /app/publish /p:UseAppHost=false --no-restore

# ── Stage 3: Публикация скомпилированных файлов ──
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# Создаем пользователя app если его нет (для Alpine)
RUN if ! id -u app >/dev/null 2>&1; then adduser -D app; fi
USER app

ENTRYPOINT ["dotnet", "CourseProject.Users.Presentation.dll"]