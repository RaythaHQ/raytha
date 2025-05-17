namespace Raytha.Application.Common.Attributes;

//Used to tell Swagger documentation to not show the decorated attribute in the
//the auto generated samples and documentation
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class ExcludePropertyFromOpenApiDocs : Attribute { }
