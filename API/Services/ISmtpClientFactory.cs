namespace PortalAdopcion.API.Services;

using MailKit.Net.Smtp;

// REFACTOR: Factoría de clientes SMTP para soportar reutilización de conexión en envíos por lote y testabilidad/mocking.
public interface ISmtpClientFactory
{
    ISmtpClient CreateClient();
}

public class SmtpClientFactory : ISmtpClientFactory
{
    public ISmtpClient CreateClient() => new SmtpClient();
}

