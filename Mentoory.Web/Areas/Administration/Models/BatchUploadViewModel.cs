using System.ComponentModel.DataAnnotations;
using Mentoory.Tenant.Application.Queries.ListRegistrationProjects;

namespace Mentoory.Web.Areas.Administration.Models;

public class BatchUploadViewModel
{
    [Required(ErrorMessage = "El archivo CSV es requerido.")]
    [Display(Name = "Archivo CSV")]
    public IFormFile? CsvFile { get; set; }

    [Required(ErrorMessage = "El proyecto es requerido.")]
    [Display(Name = "Proyecto")]
    public Guid ProjectExternalId { get; set; }

    public List<RegistrationProjectDto> Projects { get; set; } = [];
}
