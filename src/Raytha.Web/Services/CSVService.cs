using Raytha.Web.Areas.Admin.Views.ContentItems;
using Raytha.Application;
using System.Collections;
using System.IO;
using Raytha.Application.Common.Interfaces;
using CsvHelper;
using System.Globalization;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Raytha.Web.Services
{
    public class CSVService : ICSVService
    {
        public IEnumerable<T> ReadCSV<T>(Stream file)
        {
            var reader = new StreamReader(file);
            var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            var records = csv.GetRecords<T>();
            return records;

        }
    }
}
