namespace koala.src.Modules.Core.Dtos
 {
    public record AssignReviewersDto
    (
        List<Guid> TaskIds,
        List<ReviewerPairDto> RevierPairIds
    );

    public record ReviewerPairDto(Guid Reviewer1Id, Guid Reviewer2Id);
 }
