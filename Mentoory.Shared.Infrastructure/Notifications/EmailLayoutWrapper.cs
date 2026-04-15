using Mentoory.Shared.Application.Notifications;

namespace Mentoory.Shared.Infrastructure.Notifications;

public class EmailLayoutWrapper : IEmailLayoutWrapper
{
    public string WrapInBrandLayout(string innerHtml)
    {
        var year = DateTime.UtcNow.Year;

        return $"""
            <!DOCTYPE html>
            <html lang="es">
            <head>
                <meta charset="utf-8" />
                <meta name="viewport" content="width=device-width, initial-scale=1.0" />
                <title>Mentoory</title>
            </head>
            <body style="margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f6f9; color: #333;">
                <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="background-color: #f4f6f9;">
                    <tr>
                        <td align="center" style="padding: 30px 0;">
                            <table role="presentation" width="600" cellspacing="0" cellpadding="0" border="0" style="max-width: 600px; width: 100%;">
                                <!-- Header -->
                                <tr>
                                    <td style="background-color: #4e73df; padding: 24px 32px; border-radius: 8px 8px 0 0; text-align: center;">
                                        <h1 style="margin: 0; color: #ffffff; font-size: 24px; font-weight: 600; letter-spacing: 1px;">
                                            Mentoory
                                        </h1>
                                    </td>
                                </tr>
                                <!-- Content Card -->
                                <tr>
                                    <td style="background-color: #ffffff; padding: 32px; border-left: 1px solid #e3e6f0; border-right: 1px solid #e3e6f0;">
                                        {innerHtml}
                                    </td>
                                </tr>
                                <!-- Footer -->
                                <tr>
                                    <td style="background-color: #f8f9fc; padding: 20px 32px; border-radius: 0 0 8px 8px; border: 1px solid #e3e6f0; border-top: none; text-align: center;">
                                        <p style="margin: 0 0 8px 0; font-size: 13px; color: #858796;">
                                            Este es un mensaje automatizado de la plataforma Mentoory.
                                        </p>
                                        <p style="margin: 0; font-size: 12px; color: #b7b9cc;">
                                            &copy; {year} Mentoory. Todos los derechos reservados.
                                        </p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>
            </body>
            </html>
            """;
    }
}
