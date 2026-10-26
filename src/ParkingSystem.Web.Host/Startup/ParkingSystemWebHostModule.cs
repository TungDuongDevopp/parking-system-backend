using Abp.Modules;
using Abp.Reflection.Extensions;
using Abp.Threading.BackgroundWorkers;
using ParkingSystem.Configuration;
using ParkingSystem.Reservations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace ParkingSystem.Web.Host.Startup
{
    [DependsOn(
       typeof(ParkingSystemWebCoreModule))]
    public class ParkingSystemWebHostModule : AbpModule
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfigurationRoot _appConfiguration;

        public ParkingSystemWebHostModule(IWebHostEnvironment env)
        {
            _env = env;
            _appConfiguration = env.GetAppConfiguration();
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(typeof(ParkingSystemWebHostModule).GetAssembly());
        }

        public override void PostInitialize()
        {
            var isWorkerEnabled = _appConfiguration.GetValue<bool>("BackgroundJobs:ReservationExpirationWorker:IsEnabled", true);
            if (isWorkerEnabled)
            {
                var workManager = IocManager.Resolve<IBackgroundWorkerManager>();
                workManager.Add(IocManager.Resolve<ReservationExpirationWorker>());
            }
        }
    }
}
