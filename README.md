# FORMA — интернет-магазин

Full-stack интернет-магазин с каталогом товаров, корзиной, оформлением заказов и административной панелью. Проект построен на Angular и .NET и разделён на сервисы каталога и складских остатков, корзины, заказов и пользователей.

## Возможности

- Каталог с поиском, фильтрами, категориями, тегами и карточками товаров.
- Корзина с сохранением для авторизованных пользователей и переносом гостевой корзины.
- Оформление заказа, история и статусы заказов.
- Личный кабинет и обращения в поддержку, привязанные к заказам.
- Управление товарами, заказами и обращениями в административной панели.
- Отзывы покупателей и модерация жалоб.
- Проверка наличия товара и резервирование остатков при оформлении.
- Повторная обработка подтверждения оплаты через Outbox, чтобы восстановиться после временной недоступности складского сервиса.

## Технологии

**Frontend:** Angular 21, TypeScript, RxJS, Angular Signals, SCSS, Vitest  
**Backend:** C#, .NET 9, ASP.NET Core Web API, Entity Framework Core 9  
**Хранение данных:** PostgreSQL, Npgsql, EF Core Migrations  
**Аутентификация и интеграция:** JWT, REST API, фоновые службы, микросервисная архитектура  
**Тестирование:** Vitest, xUnit

## Структура

```text
frontend/                  Angular-приложение
src/
  Cart/                    сервис корзины
  Identity/                регистрация, вход и профили пользователей
  Order/                   заказы и поддержка
  Product/                 каталог, отзывы и учёт остатков
  Shop.Contracts/          контракты между сервисами
Shop.sln                   решение .NET
```

Сервисы .NET разделены на проекты `API`, `Application`, `Domain` и `Infrastructure`. Каждый сервис использует свою схему PostgreSQL; локально они могут работать с одной базой данных `ShopDb`.

## Требования

- .NET SDK 9
- Node.js 22 LTS и npm
- PostgreSQL 16 или совместимая версия
- Git

## Локальный запуск

### 1. Подготовьте конфигурацию API

Настройки с секретами не хранятся в Git. Для каждого API есть безопасный шаблон `appsettings.Development.example.json`. Скопируйте его рядом в `appsettings.Development.json`:

```powershell
Copy-Item .\src\Identity\Identity.API\appsettings.Development.example.json .\src\Identity\Identity.API\appsettings.Development.json
Copy-Item .\src\Product\Product.API\appsettings.Development.example.json .\src\Product\Product.API\appsettings.Development.json
Copy-Item .\src\Cart\Cart.API\appsettings.Development.example.json .\src\Cart\Cart.API\appsettings.Development.json
Copy-Item .\src\Order\Order.API\appsettings.Development.example.json .\src\Order\Order.API\appsettings.Development.json
```

В каждом файле замените `REPLACE_WITH_LOCAL_DB_PASSWORD` на пароль локальной базы PostgreSQL `ShopDb`. Установите собственный JWT-ключ длиной не менее 32 байт и используйте **одинаковые** значения `Jwt:Issuer`, `Jwt:Audience` и `Jwt:Key` во всех четырёх API. Один и тот же случайный ключ `ServiceAuthentication:ApiKey` укажите в Product и Order API. Эти файлы игнорируются Git; не снимайте их с игнорирования и не добавляйте секреты в шаблоны `.example.json`.

Создайте базу данных перед первым запуском API, например из `psql`:

```sql
CREATE DATABASE "ShopDb";
```

Миграции и схемы базы данных создаются при запуске API. Запускайте сервисы в отдельных терминалах из корня проекта:

```powershell
dotnet run --project .\src\Identity\Identity.API\Identity.API.csproj
dotnet run --project .\src\Product\Product.API\Product.API.csproj
dotnet run --project .\src\Cart\Cart.API\Cart.API.csproj
dotnet run --project .\src\Order\Order.API\Order.API.csproj
```

Локальные адреса API по умолчанию:

| Сервис | Адрес |
|---|---|
| Identity API | `http://localhost:5127` |
| Cart API | `http://localhost:5173` |
| Order API | `http://localhost:5126` |
| Product API | `http://localhost:5275` |

### 2. Запустите frontend

API-адреса фронтенда задаются в `frontend/public/shop-config.json`. Файл уже содержит значения для локального запуска; для другого окружения замените URL сервисов.

```powershell
Set-Location .\frontend
npm ci
npm start
```

Откройте `http://localhost:4200`.

Чтобы назначить роль администратора, после регистрации откройте инструменты разработчика браузера и найдите `userId` в `sessionStorage` → `forma.auth`. Укажите это значение в `Admin:UserId` в локальной конфигурации Identity API и перезапустите сервис.

## Проверки

Из корня проекта:

```powershell
dotnet build .\Shop.sln
dotnet test .\src\Order\Order.Domain.Tests\Order.Domain.Tests.csproj
```

Тесты Angular:

```powershell
Set-Location .\frontend
npm test
```

Сборка frontend:

```powershell
Set-Location .\frontend
npm run build
```
