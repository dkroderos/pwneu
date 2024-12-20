using Bogus;
using FluentAssertions;
using Pwneu.Play.Features.Challenges;
using Pwneu.Play.Shared.Entities;
using Pwneu.Play.Shared.Extensions;
using Pwneu.Shared.Common;
using Pwneu.Shared.Contracts;

namespace Pwneu.Play.IntegrationTests.Features.Challenges;

[Collection(nameof(IntegrationTestCollection))]
public class UpdateChallengeTests(IntegrationTestsWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Handle_Should_NotUpdateChallenge_WhenCommandIsNotValid()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = new Category
        {
            Id = categoryId,
            Name = F.Lorem.Word(),
            Description = F.Lorem.Sentence()
        };
        DbContext.Add(category);
        await DbContext.SaveChangesAsync();

        var challengeIds = new List<Guid>();
        foreach (var unused in Enumerable.Range(1, 3))
        {
            var id = Guid.NewGuid();
            challengeIds.Add(id);
            DbContext.Add(new Challenge
            {
                Id = id,
                CategoryId = categoryId,
                Name = F.Lorem.Word(),
                Description = F.Lorem.Sentence(),
                Points = F.Random.Int(1, 100),
                DeadlineEnabled = F.Random.Bool(),
                Deadline = DateTime.UtcNow,
                MaxAttempts = F.Random.Int(1, 10),
                Flags = F.Lorem.Words().ToList()
            });
            await DbContext.SaveChangesAsync();
        }

        var updatedChallenges = new List<UpdateChallenge.Command>
        {
            new(challengeIds[0], string.Empty, F.Lorem.Sentence(), 50, false, DateTime.UtcNow, 5, [], F.Lorem.Words(),
                string.Empty, string.Empty),
            new(challengeIds[1], F.Lorem.Word(), string.Empty, 50, false, DateTime.UtcNow, 5, [], F.Lorem.Words(),
                string.Empty, string.Empty),
            new(challengeIds[2], F.Lorem.Word(), F.Lorem.Sentence(), 50, false, DateTime.UtcNow, 5, [], [],
                string.Empty, string.Empty),
        };

        // Act
        var updateChallenges = new List<Result>();
        foreach (var updatedChallenge in updatedChallenges)
        {
            var updateChallenge = await Sender.Send(updatedChallenge);
            updateChallenges.Add(updateChallenge);
        }

        // Assert
        foreach (var updateChallenge in updateChallenges)
            updateChallenge.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Should_NotUpdateChallenge_WhenChallengeDoesNotExists()
    {
        // Act
        var updateChallenge = await Sender.Send(new UpdateChallenge.Command(
            Id: Guid.NewGuid(),
            Name: F.Lorem.Word(),
            Description: F.Lorem.Sentence(),
            Points: 50,
            DeadlineEnabled: false,
            Deadline: DateTime.UtcNow,
            MaxAttempts: 5,
            Tags: [],
            Flags: F.Lorem.Words(), string.Empty, string.Empty));

        // Assert
        updateChallenge.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Should_GetDifferentChallengeDetails()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = new Category
        {
            Id = categoryId,
            Name = F.Lorem.Word(),
            Description = F.Lorem.Sentence()
        };
        DbContext.Add(category);
        await DbContext.SaveChangesAsync();

        var challengeId = Guid.NewGuid();
        DbContext.Add(new Challenge
        {
            Id = challengeId,
            CategoryId = categoryId,
            Name = F.Lorem.Word(),
            Description = F.Lorem.Sentence(),
            Points = F.Random.Int(1, 100),
            DeadlineEnabled = F.Random.Bool(),
            Deadline = DateTime.UtcNow,
            MaxAttempts = F.Random.Int(1, 10),
            Flags = F.Lorem.Words().ToList()
        });
        await DbContext.SaveChangesAsync();

        var challenge = new ChallengeDetailsResponse
        {
            Id = challengeId,
            CategoryId = category.Id,
            CategoryName = category.Name,
            Name = F.Lorem.Word(),
            Description = F.Lorem.Sentence(),
            Points = F.Random.Int(1, 100),
            DeadlineEnabled = F.Random.Bool(),
            Deadline = DateTime.UtcNow,
            MaxAttempts = F.Random.Int(1, 10),
            SolveCount = 0,
            Artifacts = []
        };

        // Act
        var faker = new Faker();
        var updateChallenge = await Sender.Send(new UpdateChallenge.Command(
            Id: challenge.Id,
            Name: faker.Lorem.Word(),
            Description: faker.Lorem.Sentence(),
            Points: faker.Random.Int(101, 200),
            DeadlineEnabled: true,
            Deadline: DateTime.Now,
            MaxAttempts: faker.Random.Int(11, 20),
            Tags: [],
            Flags: faker.Lorem.Words(), string.Empty, string.Empty));

        var updatedChallenge = await DbContext
            .Challenges
            .GetDetailsByIdAsync(challenge.Id);

        // Assert
        updateChallenge.IsSuccess.Should().BeTrue();
        updatedChallenge.Should().NotBeNull();
        updatedChallenge.Should().NotBeEquivalentTo(challenge);
    }

    [Fact]
    public async Task Handle_Should_InvalidateChallengeCache()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = new Category
        {
            Id = categoryId,
            Name = F.Lorem.Word(),
            Description = F.Lorem.Sentence()
        };
        DbContext.Add(category);
        await DbContext.SaveChangesAsync();

        var challenge = new Challenge
        {
            Id = Guid.NewGuid(),
            CategoryId = categoryId,
            Name = F.Lorem.Word(),
            Description = F.Lorem.Sentence(),
            Points = F.Random.Int(1, 100),
            DeadlineEnabled = F.Random.Bool(),
            Deadline = DateTime.UtcNow,
            MaxAttempts = F.Random.Int(1, 10),
            Flags = F.Lorem.Words().ToList()
        };
        DbContext.Add(challenge);
        await DbContext.SaveChangesAsync();

        // Act
        await Sender.Send(new UpdateChallenge.Command(
            Id: challenge.Id,
            Name: F.Lorem.Word(),
            Description: F.Lorem.Sentence(),
            Points: 50,
            DeadlineEnabled: false,
            Deadline: DateTime.UtcNow,
            MaxAttempts: 5,
            Tags: [],
            Flags: F.Lorem.Words(), string.Empty, string.Empty));

        var challengeCache = Cache.GetOrDefault<Challenge>($"{nameof(Challenge)}:{challenge.Id}");

        // Assert
        challengeCache.Should().BeNull();
    }
}