# Trabajo Práctico Integrador - Desarrollo de Software 2026

## Comisión: 3K03

## Integrantes

| Nombre y Apellido | Legajo |
|---|---|
| Campos, Ana Sofía | 60240 |
| Latina, Juan Pablo | 60242 |
| Risso Vergés, Ornella | 60465 |
| Rojas Aguilar, Luz María | 60566 |

## Descripción

API hecha en ASP.NET Core para gestionar turnos médicos. Permite administrar médicos, especialidades y horarios de disponibilidad, y que los pacientes puedan reservar y cancelar sus propios turnos.

## Tecnologías usadas

- C# con ASP.NET Core
- Entity Framework Core (SQL Server)
- ASP.NET Core Identity + JWT para el login
- Swagger para probar los endpoints

## Qué necesitás para correrlo

- Visual Studio 2022 (con la carga de trabajo de ASP.NET y desarrollo web)
- SQL Server LocalDB (viene instalado con Visual Studio)

## Cómo configurarlo

1. Clonar el repositorio y abrir el archivo `Dsw2026Tpi.slnx` con Visual Studio.

2. Dentro del proyecto `Dsw2026Tpi.Api`, abrir el archivo `appsettings.Development.json` y revisar que tenga esto (ya viene cargado, pero por las dudas):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=(localdb)\\MSSQLLocalDB;Database=Dsw2026Tpi;Integrated Security=True;Encrypt=True;Trust Server Certificate=True"
  },
  "Admin": {
    "Email": "admin@dsw2026.com",
    "Password": "Admin123!"
  }
}
```

   Con esos datos (`Admin:Email` y `Admin:Password`) el sistema crea solo el usuario administrador la primera vez que se levanta la API, no hace falta crearlo a mano.

3. El proyecto usa dos bases de datos separadas: una para lo del negocio (médicos, turnos, etc.) y otra para los usuarios/login. Por eso hay que correr las migraciones dos veces (ver el paso siguiente).

## Cómo levantar el proyecto

1. En Visual Studio, ir a **Herramientas > Administrador de paquetes NuGet > Consola del Administrador de paquetes**.

2. Arriba de la consola hay un desplegable que dice **"Proyecto predeterminado"**. Seleccionar `Dsw2026Tpi.Data`.

3. Ejecutar estos dos comandos, uno por vez, para crear las tablas en la base de datos:

```
Update-Database -Context Dsw2026TpiDbContext
Update-Database -Context AuthenticationDbContext
```

4. En el Explorador de Soluciones, clic derecho sobre el proyecto `Dsw2026Tpi.Api` → **Establecer como proyecto de inicio**.

5. Apretar F5 (o el botón verde de Play) para correr el proyecto.

6. Se va a abrir el navegador con Swagger, donde se pueden probar todos los endpoints. Para probar los que piden estar logueado, primero hay que hacer login (ver más abajo) y pegar el token en el botón **Authorize** (arriba a la derecha, con el candado).

## Cómo funciona el login

Para poder usar la mayoría de los endpoints hay que estar autenticado con un token JWT. Los únicos endpoints que NO piden token son:

- `POST /api/auth/admin/login`
- `POST /api/auth/patient/login`
- `POST /api/auth/admin/register`

Una vez que se hace login, la API devuelve un `token`. Ese token hay que copiarlo y pegarlo en el botón **Authorize** de Swagger (con el formato `Bearer <token>`), y así queda autenticado para el resto de las pruebas.

Hay dos roles: **ADMINISTRADOR** (gestiona médicos, especialidades, disponibilidades y puede buscar turnos) y **PACIENTE** (reserva y cancela sus propios turnos).

## Endpoints implementados

Todos los endpoints, salvo los dos logins, necesitan el header `Authorization: Bearer <token>`
(ver "Cómo funciona el login" más arriba). Los ejemplos de body de abajo son los mínimos para
probar cada endpoint desde Swagger o Postman.

### Autenticación

**`POST /api/auth/admin/register`** — registra un nuevo administrador. No pide token.
```json
{ "email": "otro-admin@dsw2026.com", "password": "OtraPass123!" }
```
Responde `201 Created` con `{ "email": "otro-admin@dsw2026.com" }`.

**`POST /api/auth/admin/login`** — login del administrador.
```json
{ "email": "admin@dsw2026.com", "password": "Admin123!" }
```
Responde `{ "token": "...", "role": "ADMINISTRADOR" }`.

**`POST /api/auth/patient/login`** — login del paciente, sin contraseña. Si el DNI no existe
todavía, se registra automáticamente.
```json
{ "email": "paciente@mail.com", "dni": 40123456 }
```
Responde `{ "token": "...", "role": "PACIENTE" }`.

### Especialidades (Administrador)

| Método | Endpoint | Uso |
|---|---|---|
| GET | `/api/specialties?pageSize=10&pageIndex=0&name=` | Lista, paginado y con filtro opcional por nombre. |
| POST | `/api/specialties` | Crea una especialidad. |
| PUT | `/api/specialties/{id}` | Edita una especialidad existente. |
| DELETE | `/api/specialties/{id}` | Da de baja lógica (no se borra de la base). |

Body de `POST`/`PUT`:
```json
{ "name": "Cardiología", "description": "Enfermedades del corazón" }
```

### Médicos (Administrador)

| Método | Endpoint | Uso |
|---|---|---|
| GET | `/api/doctors?pageSize=10&pageIndex=0&name=` | Lista, paginado y con filtro por nombre. |
| GET | `/api/doctors/{id}/availabilities` | Horarios de atención del médico (por día de la semana). |
| POST | `/api/doctors` | Da de alta un médico. |
| PUT | `/api/doctors/{id}` | Edita un médico. |
| DELETE | `/api/doctors/{id}` | Da de baja lógica. |

Body de `POST`/`PUT` (`specialtyId` sale del `id` que devuelve `POST /api/specialties`):
```json
{ "name": "Dr. Juan Pérez", "licenseNumber": "MP12345", "specialtyId": "GUID-de-la-especialidad" }
```

### Disponibilidades (Administrador)

**`POST /api/availabilities`** — carga los días/horarios en que atiende un médico. Genera
automáticamente los turnos de 30 minutos para el resto del mes en curso, salteando feriados.
**`PUT /api/availabilities`** — mismo body, reemplaza todo lo cargado para ese mes.

```json
{
  "doctorId": "GUID-del-medico",
  "days": [
    { "day": "LUNES", "startTime": "09:00", "endTime": "12:00" },
    { "day": "MIERCOLES", "startTime": "14:00", "endTime": "17:00" }
  ]
}
```

Los `id` de los turnos generados (`availabilitySlotId`, necesarios para reservar) se consultan
en la tabla `AvailabilitySlots` de la base.

### Turnos

**`POST /api/appointments`** (Paciente) — reserva un turno.
```json
{
  "doctorId": "GUID-del-medico",
  "availabilitySlotId": "GUID-del-turno-disponible",
  "patient": { "dni": 40123456 },
  "reason": "Control de rutina"
}
```

| Método | Endpoint | Quién | Uso |
|---|---|---|---|
| GET | `/api/appointments/patient?dni={dni}` | Paciente | Sus propios turnos activos. |
| DELETE | `/api/appointments/{id}` | Paciente | Cancela un turno propio (solo si está `BOOKED`). |
| GET | `/api/appointments?date=YYYY-MM-DD` | Administrador | Turnos de un día puntual. |
| GET | `/api/appointments/search?specialtyId=&doctorId=&dni=&date=` | Administrador | Búsqueda combinada, todos los filtros son opcionales. |


## Manejo de errores

Cuando algo falla, la API siempre devuelve el error con el mismo formato, para que sea fácil de leer:

```json
{
  "errorCode": "APPOINTMENT_CONFLICT",
  "message": "El turno ya fue reservado",
  "details": [
    { "field": "dateTime", "issue": "slot_unavailable" }
  ]
}
```

## Límite de solicitudes (Rate Limiting)

Para evitar abuso, algunos endpoints tienen un límite de cuántas veces se pueden llamar por minuto:

- Login de administrador: 5 por minuto
- Login de paciente: 10 por minuto
- Reservar turno: 5 por minuto por paciente
- El resto de los endpoints: 100 por minuto

Si se supera el límite, la API responde con el código `429`.

## Logs

Los logs se guardan en la carpeta `Dsw2026Tpi.Api/Logs/`, un archivo por día.
