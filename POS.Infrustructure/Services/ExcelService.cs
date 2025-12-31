using OfficeOpenXml;
using OfficeOpenXml.Style;
using POS.Application.Contracts.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace POS.Infrustructure.Services
{
    public class ExcelService : IExcelService
    {
        public ExcelService()
        {
            // Set EPPlus License Context (NonCommercial or Commercial)
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public async Task<byte[]> ExportToExcelAsync<T>(IEnumerable<T> data, string sheetName) where T : class
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add(sheetName);

            // Get properties
            var properties = typeof(T).GetProperties()
                .Where(p => p.PropertyType.IsValueType || p.PropertyType == typeof(string))
                .ToArray();

            // Add headers
            for (int i = 0; i < properties.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = properties[i].Name;
                worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                worksheet.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                worksheet.Cells[1, i + 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            // Add data
            int row = 2;
            foreach (var item in data)
            {
                for (int i = 0; i < properties.Length; i++)
                {
                    var value = properties[i].GetValue(item);
                    worksheet.Cells[row, i + 1].Value = value;
                }
                row++;
            }

            // Auto-fit columns
            worksheet.Cells.AutoFitColumns();

            return await package.GetAsByteArrayAsync();
        }

        public async Task<IEnumerable<T>> ImportFromExcelAsync<T>(Stream excelStream) where T : class, new()
        {
            var results = new List<T>();

            if (excelStream.CanSeek)
            {
                excelStream.Position = 0;
            }

            using var package = new ExcelPackage(excelStream);
            var worksheet = package.Workbook.Worksheets[0];

            if (worksheet.Dimension == null)
                return results;

            var properties = typeof(T).GetProperties()
                .Where(p => p.CanWrite && (p.PropertyType.IsValueType || p.PropertyType == typeof(string)))
                .ToArray();

            // Read headers (row 1)
            var headers = new Dictionary<string, int>();
            for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
            {
                var header = worksheet.Cells[1, col].Value?.ToString();
                if (!string.IsNullOrEmpty(header))
                {
                    headers[header] = col;
                }
            }

            // Read data (starting from row 2)
            for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
            {
                var item = new T();
                bool hasData = false;

                foreach (var prop in properties)
                {
                    if (headers.TryGetValue(prop.Name, out int col))
                    {
                        var value = worksheet.Cells[row, col].Value;
                        if (value != null)
                        {
                            try
                            {
                                var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                                var convertedValue = Convert.ChangeType(value, targetType);
                                prop.SetValue(item, convertedValue);
                                hasData = true;
                            }
                            catch
                            {
                                // Skip invalid values
                            }
                        }
                    }
                }

                if (hasData)
                    results.Add(item);
            }

            return await Task.FromResult(results);
        }

        public async Task<byte[]> ExportReportAsync(string reportType, object parameters)
        {
            // TODO: Implement based on report type
            throw new NotImplementedException();
        }
    }
}
