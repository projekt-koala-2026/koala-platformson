namespace koala.src.Modules.Core.Dtos
{
    public record AutoCheckProgressDto(int Total, int ReadyToCheck, int MissingKey, int AlreadyReviewedWithoutResult);
}
