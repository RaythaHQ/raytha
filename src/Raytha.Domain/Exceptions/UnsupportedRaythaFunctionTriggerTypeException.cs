using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raytha.Domain.Exceptions;

public class UnsupportedRaythaFunctionTriggerTypeException : Exception
{
    public UnsupportedRaythaFunctionTriggerTypeException(string developerName) : base($"Raytha function trigger type \"{developerName}\" is unsupported.")
    {
    }
}