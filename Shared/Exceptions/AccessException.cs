namespace PortalAdopcion.Shared.Exceptions;
public class AccessException(string message) : Exception(message);
public class AuthenticationException(string message) : Exception(message);
