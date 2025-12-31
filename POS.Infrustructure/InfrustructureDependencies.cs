using Microsoft.Extensions.DependencyInjection;
using POS.Application.Contracts.Services;
using POS.Infrustructure.Services;

namespace POS.Infrustructure
{
    public static class InfrustructureDependencies
    {
        public static IServiceCollection AddInfrustructureDependencies(this IServiceCollection services)
        {
            services.AddScoped<IExcelService, ExcelService>();
            services.AddScoped<ICustomerLedgerService, CustomerLedgerService>();
            return services;
        }
    }
}
