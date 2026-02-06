namespace AuthSystemApi.Exceptions
{
    public class BusinessLogicException : Exception
    {
        public BusinessLogicException(string message) : base(message) { }
        public BusinessLogicException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class ValidationException : Exception
    {
        public ValidationException(string message) : base(message) { }
        public ValidationException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class DuplicateResourceException : Exception
    {
        public DuplicateResourceException(string message) : base(message) { }
        public DuplicateResourceException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class DatabaseException : Exception
    {
        public DatabaseException(string message) : base(message) { }
        public DatabaseException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class EmailException : Exception
    {
        public EmailException(string message) : base(message) { }
        public EmailException(string message, Exception innerException) : base(message, innerException) { }
    }
}