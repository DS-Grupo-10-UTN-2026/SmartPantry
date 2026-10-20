using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;
using Volo.Abp.Uow;

namespace SmartPantry.ExpirationWarnings;

public class ExpirationWarningWorker : AsyncPeriodicBackgroundWorkerBase
{
    public ExpirationWarningWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory)
        : base(timer, serviceScopeFactory)
    {
        Timer.Period = 60 * 60 * 1000;   // cada 1 hora (en milisegundos)
        Timer.RunOnStart = true;         // también corre al iniciar la aplicación
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        try
        {
            // Se resuelve en cada ejecución, desde el alcance (scope) de esta corrida.
            var unitOfWorkManager = workerContext.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            var manager = workerContext.ServiceProvider.GetRequiredService<ExpirationWarningManager>();

            // Criterio UTC: el día de hoy se calcula siempre en UTC, igual que en las pruebas.
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            using var uow = unitOfWorkManager.Begin(requiresNew: true, isTransactional: true);
            var result = await manager.ProcessAsync(today);
            await uow.CompleteAsync();

            Logger.LogInformation(
                "Vencimientos procesados ({Today}): {Created} creadas, {Updated} actualizadas, {Deactivated} desactivadas",
                today, result.Created, result.Updated, result.Deactivated);
        }
        catch (Exception ex)
        {
            // No se relanza: la próxima ejecución del temporizador sigue normal.
            Logger.LogError(ex, "Falló el procesamiento de vencimientos");
        }
    }
}