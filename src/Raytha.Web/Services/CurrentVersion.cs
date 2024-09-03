using Raytha.Application.Common.Interfaces;
namespace Raytha.Web.Services;

public class CurrentVersion : ICurrentVersion
{
    public string Version => "1.3.0";
}
