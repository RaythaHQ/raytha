using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Xml;

namespace Raytha.Application.Common.Interfaces
{
    public interface ICSVService
    {
        public List<Dictionary<string,object>> ReadCSV<T>(Stream file);
    }
}
