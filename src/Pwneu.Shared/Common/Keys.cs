namespace Pwneu.Shared.Common;

/// <summary>
/// Keys used for caching to avoid using wrong keys.
/// </summary>
public static class Keys
{
    // Key for caching a list of all CategoryResponse.
    public static string Categories() => "categories";

    // Key for caching a list of all category Ids.
    public static string CategoryIds() => "categoryIds";

    // Key for caching access keys.
    public static string AccessKeys() => "accessKeys";

    // Key for caching a single CategoryResponse.
    public static string Category(Guid id) => $"category:{id}";

    // Key for caching ChallengeDetailsResponse.
    public static string ChallengeDetails(Guid id) => $"challenge:{id}:details";

    // Key for caching ArtifactDataResponse.
    public static string ArtifactData(Guid id) => $"artifact:{id}:data";

    // Key for caching UserResponse.
    public static string User(string id) => $"user:{id}";

    // Key for caching UserDetailsResponse.
    public static string UserDetails(string id) => $"user:{id}:details";

    // Key for storing cache of user graph.
    public static string UserGraph(string id) => $"user:{id}:graph";

    // Key for storing cache of user graph.
    public static string UserSolveIds(string id) => $"user:{id}:solves";

    public static string Members() => "members";

    // Key for storing cache of active user ids.
    public static string ActiveUserIds() => "user:ids:active";

    // Key for caching UserCategoryEvalResponse.
    public static string UserCategoryEval(
        string userId,
        Guid categoryId) => $"user:{userId}:category:{categoryId}:eval";

    // Key for getting cache of all user's evaluation in a single category.
    public static string AllUsersEvalInCategory(Guid categoryId) => $"*user:*:category:{categoryId}:eval*";

    // Key for caching challenge flags.
    public static string Flags(Guid challengeId) => $"challenge:{challengeId}:flag";

    // Key for caching if the user has already solved the challenge.
    public static string HasSolved(string userId, Guid challengeId) => $"hasSolved:{userId}:{challengeId}";

    // Key for caching the count of the user's recent submissions.
    public static string RecentSubmits(string userId, Guid challengeId) => $"recentSubmits:{userId}:{challengeId}";

    // Key for caching the number of attempts left by the user.
    public static string AttemptsLeft(string userId, Guid challengeId) => $"attemptsLeft:{userId}:{challengeId}";
}