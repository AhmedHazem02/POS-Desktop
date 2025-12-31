using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace POS.Application.Contracts.Services
{
    public interface IExcelService
    {
        /// <summary>
        /// ????? ?????? ??? ??? Excel
        /// </summary>
        Task<byte[]> ExportToExcelAsync<T>(IEnumerable<T> data, string sheetName) where T : class;
        
        /// <summary>
        /// ??????? ?????? ?? ??? Excel
        /// </summary>
        Task<IEnumerable<T>> ImportFromExcelAsync<T>(Stream excelStream) where T : class, new();
        
        /// <summary>
        /// ????? ????? ????
        /// </summary>
        Task<byte[]> ExportReportAsync(string reportType, object parameters);
    }
}
