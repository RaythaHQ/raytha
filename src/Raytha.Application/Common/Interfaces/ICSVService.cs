using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using System.Collections;
namespace Raytha.Application.Common.Interfaces
{
    public interface ICSVService
    {
        public IEnumerable<T> ReadCSV<T>(Stream file);
    }
}
