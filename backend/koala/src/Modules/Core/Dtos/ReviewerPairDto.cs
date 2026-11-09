namespace koala.src.Modules.Core.Dtos
 {
    public record AssignReviewersDto
    (
        List<Guid> TaskIds,
        List<(Guid,Guid)> RevierPairIds
    );
 }