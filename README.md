\# SmartPantry



Repositorio del Trabajo Práctico Integrador de Desarrollo de Software 2026.



\## Integrantes



\- Celesia Magali - https://github.com/magalicelesia27-ux

\- Farias Dylan - https://github.com/fdylanf05-beep

\- Maquiavelo Lucas - https://github.com/LucasMaquiavelo

\- Perron Pedro - https://github.com/PedroPerron

\- Vallarino Sofia - https://github.com/vallarinosofia09



\## Arquitectura



Solución ABP Application (Layered), monolito en capas, con Angular como interfaz y Entity Framework Core + SQL Server como persistencia.



\## Requisitos



\- Visual Studio 2022 o 2026, con la carga de trabajo \*\*Desarrollo de ASP.NET y web\*\*

\- Node.js 24 LTS (24.15.0 o superior)

\- Yarn 1.22.x

\- SQL Server Developer o SQL Server Express / LocalDB (instancia local)

\- SQL Server Management Studio (SSMS)

\- ABP Studio Desktop

\- Git



\## Configuración local



La cadena de conexión se configura directamente en los siguientes archivos (usa autenticación integrada de Windows, sin contraseña, por lo que puede permanecer versionada):



\- `src/SmartPantry.DbMigrator/appsettings.json`

\- `src/SmartPantry.HttpApi.Host/appsettings.json`



```json

{

&#x20; "ConnectionStrings": {

&#x20;   "Default": "Server=(LocalDb)\\\\MSSQLLocalDB;Database=SmartPantry;Trusted\_Connection=True;TrustServerCertificate=true"

&#x20; }

}

```



\## Puesta en marcha



1\. Abrir `SmartPantry.slnx` en Visual Studio y esperar la restauración automática de paquetes NuGet.

2\. Compilar la solución (`Build > Build Solution`).

3\. Si no se usa Visual Studio, ejecutar manualmente:

abp install-libs

dotnet restore ./SmartPantry.slnx

dotnet build ./SmartPantry.slnx --configuration Debug --no-restore

4\. Establecer `SmartPantry.DbMigrator` como proyecto de inicio y ejecutar (F5) para crear la base de datos local y aplicar las migraciones.

5\. Establecer `SmartPantry.HttpApi.Host` como proyecto de inicio y ejecutar (F5).

6\. En una terminal aparte, ejecutar el frontend Angular:

cd angular

yarn install

yarn start



\### URLs locales



\- Backend (HttpApi.Host): https://localhost:44351

\- Swagger: https://localhost:44351/swagger

\- Frontend (Angular): http://localhost:4200



\### Detener los procesos



\- Backend: botón "Stop" en Visual Studio, o `Shift + F5`.

\- Frontend: `Ctrl + C` en la terminal donde corre `yarn start`.



\## Verificación



\- \*\*Backend\*\*: compila sin errores en Visual Studio (`Build: 14 succeeded, 0 failed`).

\- \*\*Base de datos\*\*: verificada en SSMS, conectando a `(localdb)\\MSSQLLocalDB`, base `SmartPantry`, con las tablas base de ABP (AbpUsers, AbpRoles, AbpPermissions, AbpAuditLogs, etc.) creadas correctamente tras ejecutar el DbMigrator.

\- \*\*CI\*\*: workflow en `.github/workflows/ci.yml` ejecuta verificación de archivos requeridos, build y test de .NET, y build y test de Angular en cada Pull Request y push a `main`, `stg` y `prod`.

## Pantalla "Mi despensa" (TP10)

Pantalla Angular (`/pantry`, protegida con el login de ABP) para listar, editar, marcar como consumidos y eliminar los ítems propios de la despensa (RF-11). Consume el BackEnd con los proxies generados por ABP (`angular/src/app/proxy`), que no se editan a mano: si cambia el contrato de la API se regeneran con `abp generate-proxy -t ng -u https://localhost:44351` (con la API en ejecución).

- **Componente:** `angular/src/app/pantry/pantry.component.ts` y `pantry.component.html`.
- **Ruta y menú:** `angular/src/app/app.routes.ts` y `angular/src/app/route.provider.ts`.
- **Textos:** claves `Pantry:*` y `Menu:Pantry` en `src/SmartPantry.Domain.Shared/Localization/SmartPantry/en.json` y `es.json`. La pantalla no tiene textos fijos.
- **BackEnd:** `PantryItemAppService` (`/api/app/pantry-item`), con `[Authorize]`. El propietario se toma siempre del usuario autenticado y un ítem ajeno responde 404, igual que uno inexistente, para no revelar que existe.
- **Datos faltantes (RF-09):** si un producto, una fecha, una cantidad o una unidad no está disponible, la pantalla muestra "Sin dato" y no inventa ningún valor. La cantidad 0 se muestra como 0.
- La interfaz oculta o deshabilita botones por usabilidad, pero eso no reemplaza la autorización: el aislamiento por usuario lo garantiza el servidor.

### Decisiones de diseño

#### Ítems consumidos

Los ítems consumidos **se conservan como historial** y no se eliminan automáticamente (regla de negocio 6):

- El listado no los incluye por defecto. El interruptor **Mostrar consumidos** los agrega (parámetro `IncludeConsumed` del servicio) y se ven diferenciados con la insignia "Consumido".
- Un ítem consumido ya no se puede editar ni volver a marcar: solo ofrece la acción **Eliminar**, que es explícita y pide confirmación.
- Los ítems consumidos dejan de generar advertencias de vencimiento (regla del worker del TP08).

#### Uso de `ListService`

El listado es paginado, por eso se usa `ListService` de `@abp/ng.core` (provisto a nivel del componente):

- `ListService` arma la consulta (página, orden y cantidad por página), espera 300 ms para unir cambios seguidos y cancela la consulta anterior cuando llega una nueva.
- El componente mantiene sus propios signals para los cuatro estados (cargando, error, vacío y resultados) y para el filtro de consumidos, que no forma parte de `ListService`.
- Los errores de la consulta se capturan dentro del callback de `hookToQuery`, porque `ListService` descarta los errores sin emitir nada (versión 10.6.0), y así la pantalla puede mostrar el estado de error con **Reintentar**.
- La paginación se muestra con botones Anterior y Siguiente de Bootstrap, con 10 ítems por página.

#### Criterio de fechas

- La fecha de vencimiento es una **fecha de calendario**, sin hora ni zona horaria: `DateOnly` en el BackEnd, columna `date` en SQL Server y texto `AAAA-MM-DD` en la API. La pantalla muestra la misma fecha que está guardada, sin conversiones, con el formato corto de la configuración regional (por ejemplo `4/11/26` en español y `11/4/26` en inglés).
- **No coincide del todo con el worker del TP08:** el worker calcula el día de hoy en **UTC** para compararlo con esa fecha. Como la fecha guardada no depende de ninguna zona, ambos usan el mismo día de calendario; la diferencia está en el "hoy" del worker, que en Argentina (UTC-3) puede adelantarse al día local entre las 21:00 y las 24:00. Se acepta esa diferencia y se deja documentada.
- En el formulario, el campo de fecha vacío significa "sin fecha". Se rechazan las fechas inexistentes, los años fuera de 1900 a 2100 y las fechas incompletas, con un mensaje localizado, y no se invoca al servidor.

#### Manejo de errores

- **Listado:** si la consulta falla, la pantalla muestra un mensaje comprensible con el botón **Reintentar**. No muestra códigos de estado ni detalles técnicos.
- **Acciones (editar, consumir y eliminar):** se invocan con `skipHandleError: true`. El motivo es evitar que el manejo predeterminado de ABP abra además un cuadro de error con detalles técnicos. El componente informa el resultado con un aviso localizado (éxito o "No se pudo completar la operación"), conserva lo que el usuario ingresó en el formulario y, si falla un consumo o una eliminación, vuelve a cargar la lista para mostrar el estado real.
- **Validación:** la interfaz repite por usabilidad las reglas del servidor (cantidad no negativa, fecha válida y unidad de hasta 20 caracteres), pero el servidor sigue siendo quien valida.
- **Sin sesión:** el acceso directo a `/pantry` redirige al login de ABP.

### Verificación del TP10

- **BackEnd:** `dotnet test .\SmartPantry.slnx --configuration Release` (75 pruebas, 12 de ellas del servicio de despensa, incluido el caso de un ítem de otro usuario).
- **Angular**, desde la carpeta `angular`:

```powershell
yarn build
yarn test --watch=false
```

  El segundo ejecuta 25 pruebas, 23 de ellas del componente "Mi despensa", con el servicio del proxy reemplazado por un mock (no dependen de la red).
- **Worker del TP08:** sigue funcionando sin usuario conectado; al iniciar la API registra `Vencimientos procesados (...)` en `src/SmartPantry.HttpApi.Host/Logs/logs.txt`.