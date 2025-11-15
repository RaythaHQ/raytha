using FluentValidation.Results;

namespace Raytha.Application.Common.Models;

public interface ICommandResponseDto<T>
{
    bool Success { get; }
    string Error { get; }
    IEnumerable<ValidationFailure> GetErrors();
    void AddErrors(IEnumerable<ValidationFailure> errors);
    public T Result { get; }
}

public record CommandResponseDto<T> : ICommandResponseDto<T>
{
    private IEnumerable<ValidationFailure> _errors = new List<ValidationFailure>();

    public T Result { get; }
    public string Error
    {
        get { return _errors != null && _errors.Any() ? string.Join(";", _errors) : string.Empty; }
    }
    public bool Success
    {
        get { return _errors == null || !_errors.Any(); }
    }

    public CommandResponseDto(T result)
    {
        Result = result;
    }

    public CommandResponseDto(IEnumerable<ValidationFailure> errors)
    {
        _errors = errors;
    }

    public CommandResponseDto(string propertyName, string error)
    {
        _errors = new List<ValidationFailure> { new ValidationFailure(propertyName, error) };
    }

    public IEnumerable<ValidationFailure> GetErrors()
    {
        return _errors;
    }

    public void AddErrors(IEnumerable<ValidationFailure> errors)
    {
        _errors = _errors.ToList().Concat(errors);
    }
}
