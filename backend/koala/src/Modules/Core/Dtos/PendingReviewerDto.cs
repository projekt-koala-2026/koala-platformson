namespace koala.src.Modules.Core.Dtos
{
    public record PendingReviewerDto(Guid ReviewerId, int Pending, List<Guid> UnavailableReplacements);
}
