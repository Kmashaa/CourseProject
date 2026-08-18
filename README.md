# Проект для курса "Продвинутая разработка на C# и .NET"

### Предварительные требования
.NET 10.0 SDK

Docker Desktop (для запуска интеграционных тестов)

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
	git switch sprint-7
    ```

4. Запустите тесты
	```
	dotnet test
	```
5. Запустите интеграционные тесты (требуется Docker)
   ```
	dotnet test CourseProject.IntegrationTests
   ```
6. Запустите проект

HTTPS:
    ```
	dotnet run --project CourseProject.Presentation --launch-profile https
    ```

HTTP:
    ```
	dotnet run --project CourseProject.Presentation --launch-profile http
    ```

7. Откройте Swagger

	HTTPS: https://localhost:7255/swagger/index.html

	HTTP: http://localhost:5030/swagger/index.html

## Управление схемой базы данных через миграции EF Core

Схема базы данных управляется миграциями Entity Framework Core. Миграции позволяют версионировать схему базы данных и применять изменения автоматически.

### Создание новой миграции

Для создания новой миграции после изменения моделей выполните:
	```
	dotnet ef migrations add <MigrationName> --project CourseProject.Infrastructure/CourseProject.Infrastructure.csproj --startup-project CourseProject.Presentation/CourseProject.Presentation.csproj	```

Например:
	```
	dotnet ef migrations add InitialCreate --project CourseProject
	```

### Применение миграций к базе данных

Для применения всех непримененных миграций к базе данных выполните:
	```
	dotnet ef database update --project CourseProject.Infrastructure/CourseProject.Infrastructure.csproj --startup-project CourseProject.Presentation/CourseProject.Presentation.csproj
	```

### Откат миграции
Для отката к предыдущей миграции выполните:
	```
	dotnet ef database update <PreviousMigrationName> --project CourseProject.Infrastructure/CourseProject.Infrastructure.csproj --startup-project CourseProject.Presentation/CourseProject.Presentation.csproj
	```

### Удаление последней миграции

Если миграция еще не была применена к базе данных:
	```
	dotnet ef migrations remove --project CourseProject.Infrastructure/CourseProject.Infrastructure.csproj
	```
	
## Описание API
### Event:
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

### Booking:
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
        Rejected = 3 //отклонено
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
	Заявки, при создании, попадают в хранилище со статусом Pending. 
	
	Фоновый обработчик ходит туда каждые 2 секунды и получает все заявки со статусом Pending.
	
	Обрабатывает их ("обращение в стороннюю систему" в течение 5 секунд, присвоение статуса Confirmed, присвоение даты и времени обработки заявки, обновление заявки в хранилище).

## Пример сценария использования
	Пользователь создает событие через POST /events;

	Создает бронь через POST /events/{id}/book и получает Location, по которому может следить за статусом своей заявки;

	Пользователь сразу же запрашивает информацию о своей заявке GET /bookings/{id} и получает статус Pending;

	Ожидает несколько секунд и повторяет запрос — статус изменился на Confirmed или Rejected.

## Пример сценария овербукинга
	Пользователь создает событие через POST /events с количеством мест (TotalSeats) = 3;

	Создает три брони через POST /events/{id}/book и получает Location для каждой, по которому может следить за статусом заявок;

	Пользователь создает еще одну бронь через POST /events/{id}/book и получает статус 409 Conflict из-за отсутствия свободных мест.
	
## Архитектура CourseProject
### CourseProject.Domain
Независимый слой

	Entities: Доменные сущности и бизнес-модели
	
	Exceptions: Классы кастомных доменных исключений

### CourseProject.Application
Зависит от Domain

	Exceptions: Классы кастомных исключений
	
	Extensions: Настройка DI
	
	Services: Реализация сценариев использования
	
	Models: Прикладные модели данных (DTO)

	Interfaces: Интерфейсы

### CourseProject.Infrastructure
Зависит от Domain и Application

	DataAccess: Логика хранения данных и конфигурации таблиц (Configurations)
	
	Repositories: Инфраструктурный слой (репозитории) работы с базой данных
	
	Migrations: Миграции Entity Framework Core для управления схемой базы данных
	
	Extensions: Настройка DI

### CourseProject.Presentation
Точка входа в приложение. Зависит от Application и инициализирует Infrastructure

	Controllers: Обработка HTTP-запросов
	
	Interfaces: Интерфейсы
	
	Services: Маппинг
	
	Extensions: Настройка DI

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
