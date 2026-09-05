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

