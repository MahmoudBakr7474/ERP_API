namespace WarehouseERP.SharedKernel.Results;

/// <summary>Kind of an expected failure; the HTTP boundary maps each kind to one status code.</summary>
public enum ErrorType
{
    /// <summary>Input is malformed or out of range (400).</summary>
    Validation,
    /// <summary>The addressed resource does not exist (404).</summary>
    NotFound,
    /// <summary>The request clashes with current state: a taken key or a stale version (409).</summary>
    Conflict,
    /// <summary>Input is well formed but a business rule refuses it (422).</summary>
    BusinessRule,
    /// <summary>No authenticated user (401).</summary>
    Unauthorized,
    /// <summary>The user is authenticated but not allowed (403).</summary>
    Forbidden,
}

/// <summary>One field-level input failure.</summary>
public sealed record ValidationError(string Property, string Code, string Message);

/// <summary>
/// An expected failure. <see cref="Code"/> is stable (clients branch on it, never on <see cref="Message"/>),
/// spelled <c>Module.Subject.Reason</c>, e.g. <c>Catalog.Product.SkuTaken</c>.
/// </summary>
public sealed class Error
{
    private Error(string code, string message, ErrorType type, IReadOnlyList<ValidationError> validationErrors)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        Code = code;
        Message = message;
        Type = type;
        ValidationErrors = validationErrors;
    }

    public string Code { get; }
    public string Message { get; }
    public ErrorType Type { get; }

    /// <summary>Field failures; empty unless <see cref="Type"/> is <see cref="ErrorType.Validation"/>.</summary>
    public IReadOnlyList<ValidationError> ValidationErrors { get; }

    public const string ValidationCode = "Validation.Failed";

    public static Error Validation(IReadOnlyList<ValidationError> errors) =>
        new(ValidationCode, "One or more fields are invalid.", ErrorType.Validation, errors);

    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound, []);
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict, []);
    public static Error BusinessRule(string code, string message) => new(code, message, ErrorType.BusinessRule, []);
    public static Error Unauthorized(string code, string message) => new(code, message, ErrorType.Unauthorized, []);
    public static Error Forbidden(string code, string message) => new(code, message, ErrorType.Forbidden, []);

    public override string ToString() => $"{Type} {Code}: {Message}";
}
