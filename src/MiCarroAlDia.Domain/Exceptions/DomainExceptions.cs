namespace MiCarroAlDia.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception innerException) : base(message, innerException) { }
}

public class DomainValidationException : DomainException
{
    public DomainValidationException(string message) : base(message) { }
}

public class DomainConflictException : DomainException
{
    public DomainConflictException(string message) : base(message) { }
}

public class DomainNotFoundException : DomainException
{
    public DomainNotFoundException(string message) : base(message) { }
}
