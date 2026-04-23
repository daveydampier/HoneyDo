namespace HoneyDo.Common.Exceptions;

/// <summary>
/// Use only when the caller's identity is known but their role is insufficient
/// (e.g. Contributor attempting an Owner-only action).
/// For ownership checks on items/lists, throw NotFoundException instead to avoid ID leaking.
/// </summary>
public class ForbiddenException(string message = "You do not have permission to perform this action.") : Exception(message);
