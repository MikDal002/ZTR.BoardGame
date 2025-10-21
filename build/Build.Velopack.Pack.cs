using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Git;
using Nuke.Common.Utilities;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;

partial class Build
{
    [NuGetPackage("vpk", "vpk.dll")] public readonly Tool Vpk;

    string Channel => $"{OperationSystem}-{SystemArchitecture}" + (GitVersion.PreReleaseLabel.IsNullOrWhiteSpace() ? "" : "-alpha");

    AbsolutePath VelopackRootDirectory = RootDirectory / "Velopack";
    AbsolutePath VelopackPublish => VelopackRootDirectory / "publish";

    Target CleanVelopack => _ => _
        .TriggeredBy(Clean)
        .Executes(() =>
        {
            VelopackRootDirectory.DeleteDirectory();
        });

    Target PackWithVelopack => _ => _
        .DependsOn(ConfigureAppSettings)
        .DependsOn(Publish)
        .DependsOn(CleanVelopack)
        .Executes(() =>
        {
            var iconPath = ProjectToPublish.Directory / "applogo.ico";
            Assert.True(iconPath.FileExists());

            var releaseNotesFilePath = GenerateAndSaveChangelogFile();

            Vpk.Invoke($"vpk [{OperationSystem}] pack -u {NameOfProjectToBePublished} -v {GitVersion.FullSemVer}" +
                       $" -p {PublishDirectory} --icon {iconPath} --outputDir {VelopackPublish}" +
                       $" --runtime {OperationSystem}-{SystemArchitecture} --channel {Channel} --delta none"
                       + $" --releaseNotes {releaseNotesFilePath}"
                       );
        });

    AbsolutePath GenerateAndSaveChangelogFile()
    {
        var releaseNotesFile = TemporaryDirectory / "release_notes.md";

        try
        {
            if (File.Exists(releaseNotesFile))
            {
                File.Delete(releaseNotesFile);
            }

            string commitRange;
            try
            {
                var previousTagValue = GitTasks.Git($"describe --tags --abbrev=0", logOutput: true, workingDirectory: RootDirectory)
                                          .StdToText()?.Trim();

                if (string.IsNullOrWhiteSpace(previousTagValue))
                {
                    Log.Warning("'git describe' did not return a valid tag or no tags exist. Generating changelog for all commits.");
                    commitRange = "HEAD";
                }
                else
                {
                    Log.Information($"Generating changelog since tag: {previousTagValue}");
                    commitRange = $"{previousTagValue}..HEAD";
                }
            }
            catch (ProcessException ex)
            {
                Log.Warning(ex, "Failed to get previous tag via 'git describe' (likely no tags exist). Generating changelog for all commits.");
                commitRange = "HEAD";
            }

            var commitLogOutput = GitTasks.Git($"log {commitRange} --pretty=\"format:%s\" --no-merges", logOutput: true, workingDirectory: RootDirectory)
                                          .StdToText();

            var formattedChangelog = FormatConventionalCommitChangelog(commitLogOutput);
            var sanitizedChangelog = SecurityElement.Escape(formattedChangelog);

            Log.Information("Changelog content:\n{Changelog}", formattedChangelog);
            File.WriteAllText(releaseNotesFile, sanitizedChangelog);
        }
        catch (System.Exception ex)
        {
            Log.Warning(ex, "Failed to retrieve raw commit subjects.");
        }

        return releaseNotesFile;
    }

    static string FormatConventionalCommitChangelog(string rawCommitMessages)
    {
        var features = new List<string>();
        var fixes = new List<string>();
        var others = new List<string>();

        // Regex to capture conventional commit types (feat, fix) with optional scope
        // Example: feat(login): add forgot password link
        // Example: fix: resolve issue with user profile
        var conventionalCommitRegex = new Regex(@"^(?<leading>[^a-zA-Z\n\r]*)(?<type>feat|fix)(?:\((?<scope>[^)]+)\))?:\s*(?<message>.+)$", RegexOptions.IgnoreCase);

        var commitMessages = rawCommitMessages.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var commitMessage in commitMessages)
        {
            var match = conventionalCommitRegex.Match(commitMessage);
            if (match.Success)
            {
                var type = match.Groups["type"].Value.ToLowerInvariant();
                var leadingChars = match.Groups["leading"].Value;
                var message = match.Groups["message"].Value.Trim();
                var displayMessage = $"{leadingChars}{message}";

                if (type == "feat")
                {
                    features.Add($"- {displayMessage}");
                }
                else if (type == "fix")
                {
                    fixes.Add($"- {displayMessage}");
                }
                else // This case is for other conventional types if regex is expanded later
                {
                    others.Add($"- {commitMessage.Trim()}");
                }
            }
            else
            {
                others.Add($"- {commitMessage.Trim()}");
            }
        }

        var markdownBuilder = new StringBuilder();
        if (features.Any())
        {
            markdownBuilder.AppendLine("### Features");
            features.ForEach(f => markdownBuilder.AppendLine(f));
            markdownBuilder.AppendLine();
        }

        if (fixes.Any())
        {
            markdownBuilder.AppendLine("### Bug Fixes");
            fixes.ForEach(f => markdownBuilder.AppendLine(f));
            markdownBuilder.AppendLine();
        }

        if (others.Any())
        {
            markdownBuilder.AppendLine("### Other Changes");
            others.ForEach(o => markdownBuilder.AppendLine(o));
            markdownBuilder.AppendLine();
        }

        if (markdownBuilder.Length == 0 && !commitMessages.Any()) // Only show if there were truly no commits
        {
            markdownBuilder.AppendLine("No changes in this version.");
        }
        else if (markdownBuilder.Length == 0 && commitMessages.Any()) // Commits exist but none matched feat/fix and others list is empty (e.g. only empty lines)
        {
            markdownBuilder.AppendLine("No significant changes in this version.");
        }

        return markdownBuilder.ToString();
    }
}
