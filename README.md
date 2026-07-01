# Проект для курса "Продвинутая разработка на C# и .NET"

### Предварительные требования
.NET 10.0 SDK

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
	git switch sprint-3
    ```

4. Запустите тесты
	```
	dotnet test
	```
5. Запустите проект

HTTPS:
    ```
	dotnet run --project CourseProject --launch-profile https
    ```

HTTP:
    ```
	dotnet run --project CourseProject --launch-profile http
    ```

6. Откройте Swagger

	HTTPS: https://localhost:7255/swagger/index.html

	HTTP: http://localhost:5030/swagger/index.html

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
	POST /events/{id}/book – бронирование собятия;

### Booking:
	GET /bookings/{id} – получение информации о бронировании

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

## Описание логики фоновой обработки заявок
	Заявки, при создании, попадают в хранилище со статусом Pending. Фоновый обработчик ходит туда каждые 2 секунды и получает все заявки со статусом Pending. Обрабатывает их ("обращение в стороннюю систему" в течение 5 секунд, присвоение статуса Confirmed, присвоение даты и времени обработки заявки, обновление заявки в хранилище).

## Пример сценария использования
Пользователь создает событие через POST /events;
Создает бронь через POST /events/{id}/book и получает Location, по которому может следить за статусом своей заявки;
Пользователь сразу же запрашивает информацию о своей заявке GET /bookings/{id} и получает статус Pending;
Ожидает несколько секунд и повторяет запрос — статус изменился на Confirmed или Rejected.
	
## Архитектура CourseProject
  Entities: Доменные сущности

  Models: Модели запросов

  Interfaces: Интерфейсы

  Data: Логика хранения данных

  Services: Слой бизнес-логики и маппинга

  Controllers: Обработка HTTP-запросов

  Exceptions: Классы кастомных исключений

  Extensions: Глобальные расширения


## Архитектура EventService.Tests
EventServiceTests.cs: тесты для сервиса EventService

BookingServiceTests.cs: тесты для сервиса BookingService


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
