using Abp.Localization;
using Abp.Modules;
using Abp.Reflection.Extensions;
using Abp.Runtime.Security;
using Abp.Timing;
using Abp.Zero;
using Abp.Zero.Configuration;
using ParkingSystem.Authorization.Roles;
using ParkingSystem.Authorization.Users;
using ParkingSystem.Configuration;
using ParkingSystem.Localization;
using ParkingSystem.MultiTenancy;
using ParkingSystem.Timing;

namespace ParkingSystem;

[DependsOn(typeof(AbpZeroCoreModule))]
public class ParkingSystemCoreModule : AbpModule
{
    public override void PreInitialize()
    {
        Clock.Provider = ClockProviders.Utc;
        Configuration.Auditing.IsEnabledForAnonymousUsers = true;

        // Declare entity types
        Configuration.Modules.Zero().EntityTypes.Tenant = typeof(Tenant);
        Configuration.Modules.Zero().EntityTypes.Role = typeof(Role);
        Configuration.Modules.Zero().EntityTypes.User = typeof(User);

        ParkingSystemLocalizationConfigurer.Configure(Configuration.Localization);

        // Enable this line to create a multi-tenant application.
        Configuration.MultiTenancy.IsEnabled = ParkingSystemConsts.MultiTenancyEnabled;

        // Configure roles
        AppRoleConfig.Configure(Configuration.Modules.Zero().RoleManagement);

        Configuration.Settings.Providers.Add<AppSettingProvider>();

        Configuration.Localization.Languages.Add(new LanguageInfo("fa", "فارسی", "famfamfam-flags ir"));

        Configuration.Settings.SettingEncryptionConfiguration.DefaultPassPhrase = ParkingSystemConsts.DefaultPassPhrase;
        SimpleStringCipher.DefaultPassPhrase = ParkingSystemConsts.DefaultPassPhrase;
    }

    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(ParkingSystemCoreModule).GetAssembly());
    }

    public override void PostInitialize()
    {
        IocManager.Resolve<AppTimes>().StartupTime = Clock.Now;
    }
}
