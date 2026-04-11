using CsvHelper.Configuration;

namespace Mentoory.Web.Areas.Administration.Infrastructure;

public sealed class CsvUserRecord
{
    public string Country { get; set; } = string.Empty;
    public string Identification { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

public sealed class CsvUserMap : ClassMap<CsvUserRecord>
{
    public CsvUserMap()
    {
        Map(m => m.Country).Name("Country", "Pais", "País");
        Map(m => m.Identification).Name("Identification", "Identificacion", "Identificación", "NationalId");
        Map(m => m.Email).Name("Email", "Correo");
        Map(m => m.FirstName).Name("FirstName", "Nombre");
        Map(m => m.LastName).Name("LastName", "Apellido");
    }
}
