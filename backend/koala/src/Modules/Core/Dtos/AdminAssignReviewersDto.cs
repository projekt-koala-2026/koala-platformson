namespace koala.src.Modules.Core.Dtos
{
    public record AdminAssignReviewersDto
    (
        List<(Guid, Guid)> ReviewerPairIds,
        List<Guid> TaskIds
    );
}