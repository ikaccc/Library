namespace Library.Lending.Domain.Common;

/// <summary>
/// Classifies why an operation failed. The transport layers map each type to a status code.
/// </summary>
public enum ErrorType
{
    /// <summary>The caller supplied malformed or out of range input.</summary>
    Validation,

    /// <summary>A referenced entity does not exist.</summary>
    NotFound,

    /// <summary>The request clashes with the current state, e.g. a duplicate or an operation already performed.</summary>
    Conflict,

    /// <summary>The request is well formed but a business rule blocks it.</summary>
    PreconditionFailed,
}

public sealed record Error(string Code, string Message, ErrorType Type)
{
    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);

    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);

    public static Error PreconditionFailed(string code, string message) => new(code, message, ErrorType.PreconditionFailed);
}
