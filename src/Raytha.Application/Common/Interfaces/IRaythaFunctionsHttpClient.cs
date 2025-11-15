namespace Raytha.Application.Common.Interfaces;

public interface IRaythaFunctionsHttpClient
{
    dynamic Get(string url, IDictionary<string, object> headers = null);
    dynamic Post(
        string url,
        IDictionary<string, object> headers = null,
        IDictionary<string, object> body = null,
        bool json = true
    );
    dynamic Put(
        string url,
        IDictionary<string, object> headers = null,
        IDictionary<string, object> body = null,
        bool json = true
    );
    dynamic Delete(string url, IDictionary<string, object> headers = null);
}
