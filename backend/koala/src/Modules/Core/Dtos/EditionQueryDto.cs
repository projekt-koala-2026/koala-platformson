using koala.src.Modules.Core.Entities;

namespace koala.src.Modules.Core.Dtos
{
    public record EditionQueryDto
    {
        public List<string> States { get; init; } =
        [
            EditionStates.STATE_ACTIVE,
            EditionStates.STATE_INACTIVE,
            EditionStates.STATE_CLOSED
        ];
    }
}