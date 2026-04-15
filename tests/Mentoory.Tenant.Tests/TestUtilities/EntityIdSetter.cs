using Mentoory.Shared.Domain.SeedWork;

namespace Mentoory.Tenant.Tests.TestUtilities;

internal static class EntityIdSetter
{
    public static void SetId(Entity entity, long id) =>
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(entity, id);
}
