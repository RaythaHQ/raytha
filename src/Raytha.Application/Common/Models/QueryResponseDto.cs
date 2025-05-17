using FluentValidation.Results;

namespace Raytha.Application.Common.Models;

public interface IQueryResponseDto<T>
{
    bool Success { get; }
    string Error { get; }
    IEnumerable<ValidationFailure> GetErrors();
    T Result { get; }
}

public record QueryResponseDto<T> : IQueryResponseDto<T>
{
    private IEnumerable<ValidationFailure> _errors;

    public T Result { get; }
    public string Error
    {
        get { return _errors != null && _errors.Any() ? string.Join(";", _errors) : string.Empty; }
    }
    public bool Success
    {
        get { return _errors == null || !_errors.Any(); }
    }

    public QueryResponseDto(T result)
    {
        Result = result;
    }

    public QueryResponseDto(IEnumerable<ValidationFailure> errors)
    {
        _errors = errors;
    }

    public QueryResponseDto(string propertyName, string error)
    {
        _errors = new List<ValidationFailure> { new ValidationFailure(propertyName, error) };
    }

    public IEnumerable<ValidationFailure> GetErrors()
    {
        return _errors;
    }
}
