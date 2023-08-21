using Raytha.Web.Areas.Admin.Views.ContentItems;
using Raytha.Application;
using System.Collections;
using System.IO;
using Raytha.Application.Common.Interfaces;
using CsvHelper;
using System.Globalization;
using System.Collections.Generic;

namespace Raytha.Web.Services
{
    public class CSVService : ICSVService
    {
        public List<Dictionary<string,object>> ReadCSV<T>(Stream stream)
        {
            var records = new List<Dictionary<string, object>>();

            using (var reader = new StreamReader(stream))
            using (var csv = new CsvReader(reader, new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)))
            {
                csv.Read(); 

                string[] headers = csv.Parser.Record;
                while (csv.Read())
                {
                    var record = new Dictionary<string, object>();

                    for (int i = 0; i < headers.Length; i++)
                    {
                        var value = csv.GetField(i);
                        record[headers[i]] = value;
                    }

                    records.Add(record);
                }
            }

            return records;
        }
    }
}
