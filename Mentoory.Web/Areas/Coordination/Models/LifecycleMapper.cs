using Mentoory.Tenant.Application.Projects.Queries.GetProjectLifecycle;
using Riok.Mapperly.Abstractions;

namespace Mentoory.Web.Areas.Coordination.Models;

[Mapper]
public partial class LifecycleMapper
{
    public partial LifecycleProjectViewModel ToViewModel(ProjectLifecycleDto dto);

    public partial LifecycleStageViewModel ToViewModel(ProjectLifecycleStageDto dto);

    public partial StageActionViewModel ToViewModel(StageActionDto dto);
}
