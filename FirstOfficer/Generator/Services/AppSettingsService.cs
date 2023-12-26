using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Configuration;

namespace FirstOfficer.Generator.Services
{
    internal class AppSettingsService
    {
        public IConfiguration GetAppSettings(GeneratorExecutionContext context)
        {

            if (!context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.projectdir", out var path))
            {
                throw new Exception("Could not find appsettings.Development.json");

            }
            var appDevSettingsPath = Path.Combine(path, "appsettings.Development.json");
            var appSettindgPath = Path.Combine(path, "appsettings.Development.json");
            var builder = new ConfigurationBuilder();
            builder.AddJsonFile(appDevSettingsPath, true, true);
            builder.AddJsonFile(appSettindgPath, false, true);
            return builder.Build();

        }
    }
}
