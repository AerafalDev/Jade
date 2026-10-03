#!/usr/bin/env dotnet
// Applies .github/labels.yml to the repository's labels through the GitHub CLI (gh), signed in. It creates missing
// labels and updates colors and descriptions. A label listed under `aliases` is renamed rather than replaced, so its
// issues and pull requests keep it. Labels the file does not declare are listed, and deleted only with --delete. Before
// calling GitHub, it checks that .github/labeler.yml, .github/dependabot.yml and the issue forms only apply declared
// labels: the labeler cannot create one with its token, and Dependabot skips a missing one. A second run changes nothing.
//
// Usage: dotnet scripts/sync-labels.cs [--repo <owner/name>] [--delete] [--dry-run]
//   --repo     the repository to sync (defaults to the one gh resolves from this clone)
//   --delete   also delete the labels that .github/labels.yml does not declare
//   --dry-run  print the changes without making them

#:package YamlDotNet

#:include sync-labels/Gh.cs
#:include sync-labels/GitHubLabels.cs
#:include sync-labels/Label.cs
#:include sync-labels/LabelChange.cs
#:include sync-labels/LabelChangeKind.cs
#:include sync-labels/LabelFile.cs
#:include sync-labels/LabelPlan.cs
#:include sync-labels/LabelReferences.cs
#:include sync-labels/Yaml.cs

const string Usage = "Usage: dotnet scripts/sync-labels.cs [--repo <owner/name>] [--delete] [--dry-run]";

string? repository = null;
var delete = false;
var dryRun = false;
for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--repo" when i + 1 < args.Length:
            repository = args[++i];
            break;

        case "--delete":
            delete = true;
            break;

        case "--dry-run":
            dryRun = true;
            break;

        case "-h" or "--help":
            Console.WriteLine(Usage);
            return 0;

        default:
            Console.Error.WriteLine($"Unknown or incomplete argument: {args[i]}");
            Console.Error.WriteLine(Usage);
            return 2;
    }
}

var repositoryRoot = Path.GetFullPath(Path.Combine((string)AppContext.GetData("EntryPointFileDirectoryPath")!, ".."));
try
{
    var declared = LabelFile.Load(repositoryRoot);
    var problems = LabelReferences.Check(repositoryRoot, declared);
    if (problems.Count > 0)
    {
        foreach (var problem in problems)
        {
            Console.Error.WriteLine($"error: {problem}");
        }

        return 1;
    }

    Gh.EnsureAuthenticated(repositoryRoot);
    var gitHub = new GitHubLabels(repositoryRoot, repository);
    Console.WriteLine(dryRun ? $"{gitHub.ResolveRepository()} (dry run: nothing changes)" : gitHub.ResolveRepository());

    var changes = LabelPlan.Compute(declared, gitHub.List());
    foreach (var change in changes)
    {
        var target = change.Target;
        switch (change.Kind)
        {
            case LabelChangeKind.Create:
                Console.WriteLine($"create      {target!.Name}");
                if (!dryRun)
                {
                    gitHub.Create(target);
                }

                break;

            case LabelChangeKind.Update or LabelChangeKind.Rename:
                Console.WriteLine(change.Kind == LabelChangeKind.Rename
                    ? $"rename      {change.CurrentName} -> {target!.Name}"
                    : $"update      {target!.Name} ({string.Join(", ", change.Differences)})");
                if (!dryRun)
                {
                    gitHub.Update(change.CurrentName, target);
                }

                break;

            case LabelChangeKind.Undeclared when delete:
                Console.WriteLine($"delete      {change.CurrentName}");
                if (!dryRun)
                {
                    gitHub.Delete(change.CurrentName);
                }

                break;

            case LabelChangeKind.Undeclared:
                Console.WriteLine($"undeclared  {change.CurrentName} (kept; --delete removes it)");
                break;
        }
    }

    int Count(LabelChangeKind kind) => changes.Count(change => change.Kind == kind);
    var undeclared = Count(LabelChangeKind.Undeclared);
    Console.WriteLine(
        $"{declared.Count} declared: {Count(LabelChangeKind.Create)} new, {Count(LabelChangeKind.Rename)} renamed, " +
        $"{Count(LabelChangeKind.Update)} updated, {Count(LabelChangeKind.Unchanged)} unchanged. " +
        $"{undeclared} undeclared{(undeclared == 0 ? "" : delete ? ", deleted" : ", kept")}.");
    return 0;
}
catch (InvalidOperationException e)
{
    Console.Error.WriteLine($"error: {e.Message}");
    return 1;
}
