using Raytha.Web.Areas.Admin.Views.ContentItems;
using Raytha.Application;
using System.Collections;
using System.IO;
using Raytha.Application.Common.Interfaces;
using CsvReader;
using System.Globalization;
using System.Collections.Generic;

namespace Raytha.Web.Services
{
    public class CSVService : ICSVService
    {
        public List<Dictionary<string, object>> ReadCSV<T>(Stream stream)
        {
            var records = new List<Dictionary<string, object>>();

            using (CsvReader.CsvReader csvReader = new CsvReader.CsvReader(new StreamReader(stream), true))
            {
                string[] headers = csvReader.GetFieldHeaders(); // Get column headers

                while (csvReader.ReadNextRecord())
                {
                    Dictionary<string, object> record = new Dictionary<string, object>();

                    for (int columnIndex = 0; columnIndex < csvReader.FieldCount; columnIndex++)
                    {
                        string columnName = headers[columnIndex];
                        object columnValue = csvReader[columnIndex];
                        record[columnName] = columnValue;
                    }

                    records.Add(record);
                }
            }
            return records;
        }
    }
}
