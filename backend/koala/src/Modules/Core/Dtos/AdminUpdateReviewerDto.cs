namespace koala.src.Modules.Core.Dtos
{
    public record AdminUpdateReviewersDto
    (
        Guid OldReviewer,
        Guid NewReviewer
    );
}