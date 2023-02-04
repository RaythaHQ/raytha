namespace Raytha.Application.Common.Interfaces;

public interface IContentTypeInRoutePath
{
    public string ContentTypeDeveloperName { get; }

    public bool ValidateContentTypeInRoutePathMatchesValue(string developerName, bool throwExceptionOnFailure = true);

}
