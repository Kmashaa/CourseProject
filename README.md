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
	git switch sprint-2
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
	GET /events — получить список событий с поддержкой фильтрации и пагинации.
		Title (string, optional) — поиск по названию (частичное совпадение, без учета регистра).
		From (datetime, optional) — дата начала (события, начинающиеся не раньше этой даты).
		To (datetime, optional) — дата окончания (события, заканчивающиеся не позже этой даты).
		Page (int, default=1) — номер страницы (минимум 1).
		PageSize (int, default=10) — количество элементов на странице (минимум 1).
	GET /events/{id} — получить событие по id;
	POST /events — создать событие;
	PUT /events/{id} — обновить событие целиком;
	DELETE /events/{id} — удалить событие;

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

  Extensions: Методы расширения для конфигурации проекта


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
