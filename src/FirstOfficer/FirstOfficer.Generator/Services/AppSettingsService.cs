using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Configuration;

namespace FirstOfficer.Generator.Services
{
    internal class AppSettingsService
    {
        public IConfiguration GetAppSettings(GeneratorExecutionContext context)
        {

            if(!context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.projectdir", out var appSettingsPath))
            {
                throw new Exception("Could not find appsettings.Development.json");

            }
            appSettingsPath = Path.Combine(appSettingsPath, "appsettings.Development.json");
            var builder = new ConfigurationBuilder();
            builder.AddJsonFile(appSettingsPath, false, true);
            return builder.Build();
            
        }
    }
}
