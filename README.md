# Prueba de Idempotencia (ASP.NET Core)

Implementación de POST idempotente con almacenamiento persistente (EF Core + SQLite), separada por capas (Presentation, Application, Domain, Infrastructure), con replay determinista y protección por unicidad en el modelo de dominio.

**Resumen**
- Idempotencia por `Idempotency-Key` + `payloadHash` (SHA-256 del JSON canónico).
- Registro de idempotencia con dedupe (set-on-insert), estados y snapshot de respuesta.
- Replays devuelven exactamente el mismo body y status code original.
- Blindaje en dominio mediante índice único (`orderNumber`).
- Filtro de endpoint para validar `Idempotency-Key` reusable.

## Ejecutar

- Requisitos: .NET 8 SDK
- Comandos:
  - `dotnet build`
  - `dotnet run`

La base SQLite se crea automáticamente (`Data Source=idempotencia.db`). Puedes sobrescribirla con `ConnectionStrings:Default` en `appsettings*.json`.

## Capas y archivos clave

- Presentation
  - `Presentation/Endpoints/OrdersEndpoints.cs`: mapea `POST /orders`, aplica filtro, valida `Content-Type`, `Cache-Control: no-store` y delega al caso de uso.
  - `Presentation/Filters/IdempotencyKeyFilter.cs`: valida `Idempotency-Key` y lo expone vía `HttpContext.Items`.
  - `Presentation/Http/HttpHeaderNames.cs`, `Presentation/Http/IdempotencyKeyFeature.cs`, `Presentation/Http/HttpContextExtensions.cs`.
- Application
  - `Application/Orders/ICreateOrderUseCase.cs`, `Application/Orders/CreateOrderUseCase.cs`: orquestan acquire/replay/procesamiento, dominio y snapshot.
  - `Application/ApplicationServiceCollectionExtensions.cs`: registra casos de uso y opciones.
- Domain
  - `Domain/Order.cs`, `Domain/OrderRequest.cs`, `Domain/OrderResponse.cs`: entidad y contratos de dominio.
  - `Domain/IOrderRepository.cs`: puerto de persistencia del dominio de órdenes.
  - `Domain/Idempotency/*`: puerto y modelos de idempotencia (`IIdempotencyStore`, `Acquire*`, `IdempotencyRecord`, `IdempotencyOptions`, `Hashing`).
- Infrastructure
  - `Infrastructure/AppDbContext.cs`, `Infrastructure/Entities/*`: EF Core + mapeos con índices únicos.
  - `Infrastructure/Repositories/EfCoreOrderRepository.cs`: implementación de `IOrderRepository`.
  - `Infrastructure/Stores/EfCoreIdempotencyStore.cs`: implementación de `IIdempotencyStore` con EF Core.
  - `Infrastructure/ExpiredRecordsCleanupService.cs`: limpieza periódica por TTL.
  - `Infrastructure/InfrastructureServiceCollectionExtensions.cs`: DI de infra.

## Contrato HTTP

- Ruta: `POST /orders`
- Headers:
  - `Idempotency-Key`: requerido (UUID/ULID recomendado)
  - `Content-Type: application/json`
- Body:
  - `{ "orderNumber": "ORD-1001", "amount": 42.5 }`
- Respuestas:
  - 201 Created: primera creación (snapshot guardado)
  - 200 OK: existía la orden (por unicidad) o replay de éxito anterior con mismo body
  - 409 Conflict: misma key con payload distinto
  - 409 Conflict + `Retry-After`: procesamiento concurrente en curso
  - 415 Unsupported Media Type: `Content-Type` inválido
  - 400 Bad Request: falta/valor vacío de `Idempotency-Key`

Ejemplo (HTTP):

POST /orders
Idempotency-Key: 11111111-1111-1111-1111-111111111111
Content-Type: application/json

{ "orderNumber": "ORD-1001", "amount": 42.5 }

Reintentar con la misma `Idempotency-Key` y el mismo body devuelve exactamente el mismo body/status.

## Flujo de Idempotencia

- Adquisición (dedupe):
  - Se calcula `payloadHash` (SHA-256 del JSON canónico del request) y se intenta un insert atómico del registro `(op, key)` como `Processing` con TTL.
  - Si existe:
    - `payloadHash` distinto → 409 Conflict.
    - `Succeeded` → replay (mismo `responseBodyJson` + `responseStatusCode`).
    - `Processing` → 409 + `Retry-After` (o takeover si `processingTimeout` vencido vía CAS).
    - `Failed` → opcional takeover a `Processing`.
- Dominio: se ejecuta la operación (CreateOrGet) con índice único como blindaje.
- Finalización: se persiste `Succeeded` + snapshot canónico de la respuesta y status code. Replays posteriores devuelven exactamente ese snapshot.
- Limpieza: job elimina registros vencidos por `expiresAt` (TTL).

## Opciones

- `IdempotencyOptions` (configuración opcional en `Idempotency`):
  - `Ttl` (default 48h): ventana de replay y limpieza.
  - `ProcessingTimeout` (default 10m): timeout lógico para permitir takeover de `Processing` atascados.

## Cambiar el almacenamiento

- EF Core SQLite es de referencia. Para producción puede usarse:
  - SQL Server/PostgreSQL: cambiar `UseSqlite` por `UseSqlServer`/`UseNpgsql` en `InfrastructureServiceCollectionExtensions` y agregar migraciones.
  - Redis/Mongo: implementar `IIdempotencyStore` con `SET NX`/índices únicos + TTL nativo.

## Principios de diseño

- Capa Presentation: solo HTTP (validación mínima, headers, status). Sin dependencia de infra.
- Capa Application: orquesta casos de uso; no conoce EF ni detalles de persistencia.
- Capa Domain: entidades, puertos e invariantes (incluye modelos de idempotencia y contratos de órdenes).
- Capa Infrastructure: implementaciones concretas de puertos (EF Core), entidades de persistencia y servicios del sistema.

## Notas

- El filtro de endpoint para `Idempotency-Key` está diseñado para reutilizarse en cualquier POST idempotente.
- La respuesta canónica se serializa en camelCase y se usa tanto para la respuesta HTTP como para el snapshot almacenado.
