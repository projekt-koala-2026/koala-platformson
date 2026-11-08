namespace koala.src.Modules.Core.Dtos
 {
    public class ReviewerPairDto
    {
        public Guid Reviewer1Id { get; set; }
        public Guid Reviewer2Id { get; set; }
    }

    public class AssignSubEditionReviewersRequestDto
    {
        public Dictionary<Guid, List<ReviewerPairDto>> TaskReviewerPairs { get; set; } = new();
    }
 }