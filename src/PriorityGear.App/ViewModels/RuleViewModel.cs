using PriorityGear.Core;

namespace PriorityGear.App.ViewModels;

public sealed class RuleViewModel
{
    public RuleViewModel()
        : this(PriorityRule.ForExecutable("process.exe"))
    {
    }

    public RuleViewModel(PriorityRule rule)
    {
        Rule = rule;
    }

    public PriorityRule Rule { get; }

    public Guid Id => Rule.Id;

    public bool Enabled
    {
        get => Rule.Enabled;
        set => Rule.Enabled = value;
    }

    public string DisplayName
    {
        get => Rule.DisplayName;
        set => Rule.DisplayName = value;
    }

    public string? ExecutableName
    {
        get => Rule.Match.ExecutableName;
        set => Rule.Match.ExecutableName = value;
    }

    public string? FullPath
    {
        get => Rule.Match.FullPath;
        set => Rule.Match.FullPath = string.IsNullOrWhiteSpace(value) ? null : value;
    }

    public string? PathSuffix
    {
        get => Rule.Match.PathSuffix;
        set => Rule.Match.PathSuffix = string.IsNullOrWhiteSpace(value) ? null : value;
    }

    public ProcessPriorityLevel BasePriority
    {
        get => Rule.BasePriority;
        set => Rule.BasePriority = value;
    }

    public ProcessPriorityLevel ActivePriority
    {
        get => Rule.ActivePriority;
        set => Rule.ActivePriority = value;
    }

    public bool ActiveModeEnabled
    {
        get => Rule.ActiveModeEnabled;
        set => Rule.ActiveModeEnabled = value;
    }

    public RuleScope Scope
    {
        get => Rule.Scope;
        set => Rule.Scope = value;
    }

    public string? Notes
    {
        get => Rule.Notes;
        set => Rule.Notes = value;
    }
}
