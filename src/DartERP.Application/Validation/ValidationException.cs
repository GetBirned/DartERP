namespace DartERP.Application.Validation;

/// <summary>
/// Thrown when a business rule is violated. The UI layer catches this and
/// shows <see cref="Exception.Message"/> directly to the user, so messages
/// here should always be plain-language and safe to display as-is.
/// </summary>
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message)
    {
    }
}
