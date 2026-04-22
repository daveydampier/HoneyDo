namespace HoneyDo.Common.Exceptions;

public class UnauthorizedException(string message = "Authentication is required.") : Exception(message);
