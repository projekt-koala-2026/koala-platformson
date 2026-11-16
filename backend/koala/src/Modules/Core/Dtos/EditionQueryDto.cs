using koala.src.Modules.Core.Entities;

namespace koala.src.Modules.Core.Dtos
{
    public record EditionQueryDto
    {
        public List<string> States { get; init; } =
        [
            LifecycleStates.STATE_ACTIVE,
            LifecycleStates.STATE_INACTIVE,
            LifecycleStates.STATE_CLOSED
        ];
    }
}