using System.ComponentModel.DataAnnotations;

namespace Pwneu.Play.Shared.Entities;

// TODO -- Allow nullable category
// TODO -- Add "Competition" entity

public class Challenge
{
    public Guid Id { get; init; }
    public Guid CategoryId { get; init; }
    [MaxLength(100)] public string Name { get; set; } = string.Empty;
    [MaxLength(300)] public string Description { get; set; } = string.Empty;
    public int Points { get; set; }
    public bool DeadlineEnabled { get; set; }
    public DateTime Deadline { get; set; }
    public int MaxAttempts { get; set; }
    public List<string> Flags { get; set; } = [];
    public Category Category { get; init; } = null!;
    public List<Hint> Hints { get; init; } = [];
    public ICollection<Artifact> Artifacts { get; init; } = [];
    public ICollection<Submission> Submissions { get; init; } = [];
}