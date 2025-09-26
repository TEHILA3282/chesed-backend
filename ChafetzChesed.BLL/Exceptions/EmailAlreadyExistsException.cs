namespace ChafetzChesed.BLL.Exceptions;

public sealed class EmailAlreadyExistsException : Exception
{
    public EmailAlreadyExistsException(string email)
        : base($"האימייל '{email}' כבר קיים במוסד.") { }
}
