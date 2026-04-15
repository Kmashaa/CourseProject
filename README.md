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
	git switch sprint-1
    ```
4. Запустите проект

HTTPS:
    ```
	dotnet run --project CourseProject --launch-profile https
    ```

HTTP:
    ```
	dotnet run --project CourseProject --launch-profile http
    ```

5. Откройте Swagger

	HTTPS: https://localhost:7255/swagger/index.html

	HTTP: http://localhost:5030/swagger/index.html

## Описание API
	GET /events — получить список всех событий;
	GET /events/{id} — получить событие по id;
	POST /events — создать событие;
	PUT /events/{id} — обновить событие целиком;
	DELETE /events/{id} — удалить событие;

## Архитектура
  Entities: Доменные сущности

  Models: Модели запросов

  Interfaces: Интерфейсы

  Data: Логика хранения данных

  Services: Слой бизнес-логики и маппинга

  Controllers: Обработка HTTP-запросов

  Extensions: Методы расширения для конфигурации проекта
