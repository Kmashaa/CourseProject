# Проект для курса "Продвинутая разработка на C# и .NET"
Система состоит из трёх микросервисов, каждый со своей базой данных PostgreSQL. Обмен сообщениями между сервисами выполняется через Apache Kafka. Все компоненты контейнеризированы и управляются через Docker Compose. Реализовано кеширование Cache-aside через Redis.

### Инструкция
1. Клонируйте репозиторий
    ```
    git clone https://github.com/Kmashaa/CourseProject.git
    ``` 
2. Перейдите в папку пректа
    ```
	cd CourseProject
    ```
3. Перейдите в нужную ветку
    ```
	git switch sprint-10
    ```

4. Запустите проект

    ```
	docker compose up -d --build
    ```

(Для остановки и удаления контейнеров: docker compose down)

5. После запуска сервисы будут доступны по адресам:

Users API: http://localhost:7127 (http://localhost:7127/swagger/index.html)

Events API: http://localhost:7128 (http://localhost:7128/swagger/index.html)

Bookings API: http://localhost:7129 (http://localhost:7129/swagger/index.html)


## Состав системы

| Сервис             | Описание                              | Технологии                       | Порт хоста | Порт контейнера |
|--------------------|---------------------------------------|----------------------------------|------------|------------------|
| **users-service**    | Аутентификация и управление пользователями | ASP.NET Core 10, PostgreSQL      | 7127       | 8080             |
| **events-service**   | Управление событиями (мероприятиями)  | ASP.NET Core 10, PostgreSQL, Kafka | 7128       | 8080             |
| **bookings-service** | Создание бронирований                | ASP.NET Core 10, PostgreSQL, Kafka | 7129       | 8080             |


### Базы данных

Каждый сервис использует отдельную базу данных PostgreSQL 16.

| База данных   | Контейнер            | Порт хоста | Порт контейнера | Пользователь | Пароль   | Имя БД    |
|---------------|----------------------|------------|------------------|--------------|----------|-----------|
| users-db      | eventapi-users-db    | 5432       | 5432             | postgres     | postgres | users     |
| events-db     | eventapi-events-db   | 5433       | 5432             | postgres     | postgres | events    |
| bookings-db   | eventapi-bookings-db | 5434       | 5432             | postgres     | postgres | bookings  |

> Внутри сети Docker все базы данных доступны по стандартному порту `5432`. Порты хоста (`5432`, `5433`, `5434`) используются только для подключения с хоста (например, из IDE или клиента БД).

### Кешируемые данные

#### События (Events)

| Данные | Ключ | TTL | Описание |
|--------|------|-----|----------|
| Событие по ID | `events:{eventId}` | 3 минуты (ShortTTL) | Отдельное событие, запрашиваемое по ID |
| Топ событий | `events:top{number}` | 15 минут (LongTTL) | Список топ-N событий по продажам |

ShortTTL (3 мин) для конкретного события — события могут менять статус или метаданные, и пользователь ожидает видеть актуальные данные почти сразу. 3 минуты достаточно, чтобы срезать пиковые нагрузки.

LongTTL (15 мин) для топа — 15 минут дают существенную экономию ресурсов БД, при этом пользователь не заметит, что топ обновился на 10 минут позже.

### Стратегия инвалидации: Обновление при записи

#### Принцип работы

При мутирующих операциях (создание, обновление, удаление) кеш обновляется немедленно после успешной записи в базу данных.

### Что происходит при недоступном Redis:

Все операции с кешем оборачиваются в try/catch. Если Redis недоступен (timeout, connection error), приложение продолжает работать напрямую с БД.

Когда Redis снова станет доступен, кеш будет заполняться заново по мере запросов.

## Наблюдаемость (Observability)

В проект интегрированы инструменты для сбора метрик, трассировок и визуализации:

| Инструмент | Назначение                          | URL (UI)                 |
|------------|-------------------------------------|--------------------------|
| Prometheus | Сбор и хранение метрик              | http://localhost:9090    |
| Grafana    | Дашборды и визуализация метрик      | http://localhost:3000    |
| Jaeger     | Трассировка запросов                | http://localhost:16686   |

### Запуск стека мониторинга

Все сервисы наблюдаемости уже включены в `docker-compose.yml` и запускаются вместе с микросервисами:

### После старта контейнеров:

Prometheus будет автоматически скрейпить метрики с микросервисов (цели events-service, bookings-service, users-service) по пути /metrics.

Grafana запустится с provisioned data source Prometheus. Логин по умолчанию: admin, пароль: admin (задан в docker-compose.yml).

Jaeger принимает трассировки от микросервисов по OTLP (gRPC на порту 4317).


### Инфраструктура обмена сообщениями

- **Zookeeper** – координатор для Kafka (порт `2181` внутри сети).
- **Kafka** – брокер сообщений.
  - Внутренний listener: `kafka:29092` (используется сервисами внутри Docker).
  - Внешний listener: `localhost:9092` (для подключения с хоста).

## Структура Docker-образов

Каждый микросервис собирается из собственного Dockerfile, расположенного в папке `Presentation` соответствующего проекта:

- `CourseProject.Users.Presentation/Dockerfile`
- `CourseProject.Events.Presentation/Dockerfile`
- `CourseProject.Bookings.Presentation/Dockerfile`

Все Dockerfile используют многоступенчатую сборку на базе .NET 10 Alpine и включают поддержку ICU для корректной работы с локализацией.

## Конфигурация сервисов

Конфигурация задаётся через переменные окружения в `docker-compose.yml` и переопределяет значения из `appsettings.json`. Для запуска в Docker не требуется изменять сами файлы конфигурации.

### users-service
- **БД (внутри Docker):** `Host=eventapi-users-db;Port=5432;Database=users;Username=postgres;Password=postgres`
- **БД (локально):** `Host=localhost;Port=5432;Database=users;Username=postgres;Password=postgres`
- **JWT:** общий секретный ключ (см. ниже)
- **Порты:** HTTP `8080` (снаружи `7127`)

### events-service
- **БД (внутри Docker):** `Host=eventapi-events-db;Port=5432;Database=events;Username=postgres;Password=postgres`
- **БД (локально):** `Host=localhost;Port=5433;Database=events;Username=postgres;Password=postgres`
- **Kafka:** `BootstrapServers=kafka:29092`, `ConsumerGroup=events-service-group`
- **JWT:** общий секретный ключ
- **Порты:** HTTP `8080` (снаружи `7128`)

### bookings-service
- **БД (внутри Docker):** `Host=eventapi-bookings-db;Port=5432;Database=bookings;Username=postgres;Password=postgres`
- **БД (локально):** `Host=localhost;Port=5434;Database=bookings;Username=postgres;Password=postgres`
- **Kafka:** `BootstrapServers=kafka:29092`, `ConsumerGroup=bookings-service-group`
- **JWT:** общий секретный ключ
- **Порты:** HTTP `8080` (снаружи `7129`)

## Запуск системы

### Предварительные требования
- Установленный Docker и Docker Compose.
- .NET 10.0 SDK (для локальной разработки и тестов).
  

## Управление схемой базы данных через миграции EF Core

Схема базы данных управляется миграциями Entity Framework Core. Миграции позволяют версионировать схему базы данных и применять изменения автоматически.

### Создание новой миграции

Например, для events-service:
	```
	dotnet ef migrations add <MigrationName> --project CourseProject.Events.Infrastructure/CourseProject.Events.Infrastructure.csproj --startup-project CourseProject.Events.Presentation/CourseProject.Events.Presentation.csproj
	```
Аналогично для users и bookings.

### Применение миграций к базе данных

Для применения всех непримененных миграций к базе данных выполните:
	```
	dotnet ef database update --project CourseProject.Events.Infrastructure/CourseProject.Events.Infrastructure.csproj --startup-project CourseProject.Events.Presentation/CourseProject.Events.Presentation.csproj
	```

### Откат миграции
Для отката к предыдущей миграции выполните:
	```
	dotnet ef database update <PreviousMigrationName> --project CourseProject.Events.Infrastructure/CourseProject.Events.Infrastructure.csproj --startup-project CourseProject.Events.Presentation/CourseProject.Events.Presentation.csproj
	```

### Удаление последней миграции

Если миграция еще не была применена к базе данных:
	```
	dotnet ef migrations remove --project CourseProject.Events.Infrastructure/CourseProject.Events.Infrastructure.csproj
	```

### Роли пользователей

Система поддерживает две роли пользователей:

User - Обычный пользователь (роль по умолчанию)
Admin - Администратор системы - Полный доступ

POST /auth/register принимает логин, пароль и необязательное поле role (User по умолчанию). Для удобства тестирования передача Admin допустима. Доступен без токена.

POST /auth/login принимает учётные данные и возвращает JWT-токен, доступен без токена.

POST /bookings/{id}/book, GET /bookings/{id} и DELETE /bookings/{id} требуют аутентификации; идентификатор текущего пользователя читается из claims и передается в сервис.

POST /events/, PUT /events/{id}, DELETE /events/{id} доступны только администраторам; Используется атрибут [Authorize(Roles = "Admin")].

Маппинг новых исключений в обработчик ошибок: нет прав → 403, событие в прошлом → 400, лимит броней → 409.

### Получение JWT-токена через Swagger
1. Регистрация пользователя
   
Найдите эндпоинт POST /auth/register в Swagger UI сервиса users-service (http://localhost:7127/swagger). 

Введите данные пользователя:

Для обычного пользователя:

	
	
	{
	  "login": "testuser",
	  "password": "password123"
	}
	
	
Для администратора (для тестирования):

	
	
	{
	  "login": "admin",
	  "password": "admin123",
	  "role": "Admin"
	}

	
Нажмите кнопку "Execute"

2. Войдите в систему

Найдите POST /auth/login, введите те же учётные данные:


	{
	  "login": "testuser",
	  "password": "password123"
	}

	
Нажмите кнопку "Execute"

В ответе вы получите JWT-токен:

	
	{
	  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
	}

	
3. Авторизуйтесь в Swagger

Скопируйте полученный токен (без кавычек)

В верхней части Swagger UI нажмите кнопку "Authorize" 

В появившемся окне введите токен в поле "Value" токен

Нажмите кнопку "Authorize"

Закройте окно

## Настройка JWT-аутентификации

### Конфигурация JWT

Генерация безопасного секрета

	
	$secret = -join ((48..57) + (65..90) + (97..122) + (33..47) + (58..64) + (91..96) + (123..126) | Get-Random -Count 64 | ForEach-Object {[char]$_})
	Write-Host $secret
	
Требования к секрету
Минимум 32 символа (рекомендуется 64+)

Содержит строчные и заглавные буквы, цифры, спецсимволы

Не хранится в репозитории

Разные секреты для разных сред



JWT-аутентификация настраивается в файле `appsettings.json`:

```json
{
  "Jwt": {
    "Secret": "your-super-secret-key-at-least-32-characters-long",
    "Issuer": "CourseProject",
    "Audience": "CourseProjectClients",
    "ExpirationMinutes": 15
  }
}
```

## Описание API
### users-service (порт 7127)
	POST /auth/register – регистрация пользователя.
	
	POST /auth/login – вход и получение JWT.
### events-service (порт 7128)
	GET /events — получить список событий с поддержкой фильтрации и пагинации.
		Параметры (query):
		Title (string, optional) — поиск по названию (частичное совпадение, без учета регистра).
		From (datetime, optional) — дата начала (события, начинающиеся не раньше этой даты).
		To (datetime, optional) — дата окончания (события, заканчивающиеся не позже этой даты).
		Page (int, default=1) — номер страницы (минимум 1).
		PageSize (int, default=10) — количество элементов на странице (минимум 1).
	GET /events/{id} — получить событие по id;
	POST /events — создать событие;
	PUT /events/{id} — обновить событие целиком;
	DELETE /events/{id} — удалить событие;
	POST /events/{id}/book – бронирование события;

### bookings-service (порт 7129)
	GET /bookings/{id} – получение информации о бронировании. Возвращает:
		200	OK
		202	Bookings was accepted successfully
		404	Event was not found
		409	Event no available seats for the event

## Описание моделей
```
    public class Booking
    {
        public required Guid Id { get; set; } //id бронирования

        public required Guid EventId { get; set; } //id события

        public required BookingStatus Status { get; set; } //статус бронирования

        public required DateTime CreatedAt { get; set; } //время создания заявки на бронирование

        public DateTime? ProcessedAt { get; set; } //время подтверждения заявки на бронирование
    }

    public enum BookingStatus
    {
        Pending = 1, //в ожидании обработки
        Confirmed = 2, //подтверждено
        Rejected = 3, //отклонено
        Cancelled = 4 //закрыто
    }
```

```
    public class Event
    {
        public required Guid Id { get; set; } //id события

        public required string Title { get; set; } //название события

        public string? Description { get; set; } //описание события

        public required DateTime StartAt { get; set; } //дата и время начала события

        public required DateTime EndAt { get; set; } //дата и время окончания события

        public required int TotalSeats { get; set; } //общее количество место

        public int AvailableSeats { get; set; } //количество свободных мест

        public bool TryReserveSeats(int count = 1) //метод резервирования мест
        {
            if (AvailableSeats >= 0 && AvailableSeats - count >= 0)
            {
                AvailableSeats -= count;
                return true;
            }
            else
            {
                throw new NoAvailableSeatsException();
            }
        }

        public bool ReleaseSeats(int count = 1) //метод возвращения мест
        {
            AvailableSeats += count;
            return true;
        }
```

## Описание примитивов синхронизации
Для синхронизации используются SemaphoreSlim(1, 1) в BookingProcessingService и BookingService.

В BookingService для проверки существования события и резервирования места для брони.

В BookingProcessingService для проверки существования события и присвоения необходимого статуса брони.

## Описание логики фоновой обработки заявок
1. При создании брони BookingService создает BookingCreated, отправляет в Kafkd. Это событие читает EventService
2. При полученном событии из п.1 EventService валидирует свое событие на возможность брони (актуально ли событие, есть ли места и существует ли такое мероприятие)
- Если это возможно, то сервис уменьшает кол-во мест и публикует событие EventSeatReserved
- Иначе посылается EventSeatUnavailable с причиной отказа
3. Оба события из п.2 вычитывает BookingService и, в зависимости от результата, публикует BookingConfirmed\BookingRejected.

## Пример сценария использования
	Администратор создает событие через POST /events;

	Создает бронь через POST /bookings/{id}/book и получает Location, по которому может следить за статусом своей заявки;

	Пользователь сразу же запрашивает информацию о своей заявке GET /bookings/{id} и получает статус Pending;

	Ожидает несколько секунд и повторяет запрос — статус изменился на Confirmed или Rejected.

## Пример сценария овербукинга
	Администратор создает событие через POST /events с количеством мест (TotalSeats) = 3;

	Создает три брони через POST /bookings/{id}/book и получает Location для каждой, по которому может следить за статусом заявок;

	Пользователь создает еще одну бронь через POST /bookings/{id}/book и получает статус 409 Conflict из-за отсутствия свободных мест.
	
## Архитектура
Каждый микросервис следует принципам чистой архитектуры и состоит из слоёв:

Domain – доменные сущности, бизнес-модели, исключения.

Application – сценарии использования, интерфейсы, DTO, настройка DI.

Infrastructure – реализация репозиториев, EF Core, миграции, конфигурации таблиц.

Presentation – контроллеры, маппинг, настройка DI, точка входа.

Пример для events-service:

CourseProject.Events.Domain

CourseProject.Events.Application

CourseProject.Events.Infrastructure

CourseProject.Events.Presentation

Аналогично для users-service и bookings-service.

Общий проект CourseProject.Contracts содержит контракты сообщений Kafka и используется сервисами для сериализации/десериализации событий.

## Архитектура CourseProject.Tests
EventServiceTests.cs: тесты для сервиса EventService

BookingServiceTests.cs: тесты для сервиса BookingService


## Архитектура CourseProject.IntegrationTests
EventRepositoryTests.cs: интеграционные тесты для репозитория EventRepository

BookingRepositoryTests.cs: интеграционные тесты для репозитория BookingRepository

### Интеграционные тесты
Интеграционные тесты используют реальную базу данных PostgreSQL, запущенную в Docker-контейнере через библиотеку Testcontainers.

**Требования для запуска:**
- Docker Desktop должен быть установлен и запущен
- Docker engine должен быть доступен

1. При запуске интеграционных тестов автоматически создается Docker-контейнер с PostgreSQL 16

2. Перед каждым тестом применяются миграции EF Core через `MigrateAsync()`

3. После каждого теста база данных очищается

4. После завершения всех тестов контейнер автоматически удаляется

  
## Обработка ошибок

API использует стандарт **Problem Details for HTTP APIs** ([RFC 7807](https://ietf.org)) для возврата информации об ошибках.

### Формат ответа при ошибке (400, 404, 500)
```
json
{
  "type": "https://ietf.org",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "traceId": "00-84b238d...-00",
  "errors": {
    "Page": [
      "Page number must be greater than 0"
    ],
    "PageSize": [
      "Page size must be greater than 0"
    ]
  }
}
```

status:	HTTP статус код ошибки

title:	Краткое описание типа ошибки

errors:	(Опционально) Список конкретных ошибок валидации для каждого поля

traceId:	Уникальный идентификатор запроса для логов
