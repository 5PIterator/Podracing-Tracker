using System.CommandLine;
using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Nodes;

static string FindRepoRoot()
{
    var dir = new DirectoryInfo(Environment.CurrentDirectory);
    while (dir is not null)
    {
        if (File.Exists(Path.Combine(dir.FullName, "PodracingTracker.sln")))
            return dir.FullName;
        dir = dir.Parent;
    }

    var baseDir = AppContext.BaseDirectory;
    dir = new DirectoryInfo(baseDir);
    while (dir is not null)
    {
        if (File.Exists(Path.Combine(dir.FullName, "PodracingTracker.sln")))
            return dir.FullName;
        dir = dir.Parent;
    }

    throw new InvalidOperationException(
        "Could not locate repository root (PodracingTracker.sln). Run from inside the Podracing Tracker repo.");
}

static string ManifestPath(string repoRoot) =>
    Path.Combine(repoRoot, "src", "PodracingTracker", "manifest.json");

static string ModProjectPath(string repoRoot) =>
    Path.Combine(repoRoot, "src", "PodracingTracker", "PodracingTracker.csproj");

static string BinDir(string repoRoot, string configuration) =>
    Path.Combine(repoRoot, "src", "PodracingTracker", "bin", configuration);

static string ZipPath(string repoRoot) =>
    Path.Combine(BinDir(repoRoot, "Release"), "TheIterator.PodracingTracker.zip");

static async Task<int> RunAsync(string fileName, string workingDirectory, params string[] arguments)
{
    using var p = new Process
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        },
    };
    foreach (var a in arguments)
        p.StartInfo.ArgumentList.Add(a);

    p.Start();
    var stdout = await p.StandardOutput.ReadToEndAsync();
    var stderr = await p.StandardError.ReadToEndAsync();
    await p.WaitForExitAsync();
    if (stdout.Length > 0)
        Console.Write(stdout);
    if (stderr.Length > 0)
        Console.Error.Write(stderr);
    return p.ExitCode;
}

static (int major, int minor, int patch) ParseSemver3(string version)
{
    var parts = version.Split('.', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length != 3)
        throw new ArgumentException($"Version must be major.minor.patch, got: {version}");
    return (int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]));
}

static string FormatSemver3(int major, int minor, int patch) => $"{major}.{minor}.{patch}";

static string BumpVersion(string current, string bumpKind)
{
    var kind = bumpKind.Trim().ToLowerInvariant();
    var (maj, min, pat) = ParseSemver3(current);
    return kind switch
    {
        "patch" => FormatSemver3(maj, min, pat + 1),
        "minor" => FormatSemver3(maj, min + 1, 0),
        "major" => FormatSemver3(maj + 1, 0, 0),
        _ => throw new ArgumentException("--bump must be patch, minor, or major."),
    };
}

static async Task<string> ReadManifestVersionAsync(string manifestPath)
{
    await using var stream = File.OpenRead(manifestPath);
    var doc = await JsonDocument.ParseAsync(stream);
    if (!doc.RootElement.TryGetProperty("version", out var v) || v.GetString() is not { } s)
        throw new InvalidOperationException("manifest.json has no string \"version\" property.");
    return s;
}

static async Task WriteManifestVersionAsync(string manifestPath, string newVersion)
{
    var text = await File.ReadAllTextAsync(manifestPath);
    var node = JsonNode.Parse(text) ?? throw new InvalidOperationException("manifest.json is empty or invalid JSON.");
    node["version"] = newVersion;
    var opts = new JsonSerializerOptions { WriteIndented = true };
    await File.WriteAllTextAsync(manifestPath, node.ToJsonString(opts) + Environment.NewLine);
}

static void CreateModZip(string binReleaseDir, string zipPath)
{
    if (!Directory.Exists(binReleaseDir))
        throw new DirectoryNotFoundException($"Build output not found: {binReleaseDir}");

    if (File.Exists(zipPath))
        File.Delete(zipPath);

    using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
    foreach (var file in Directory.EnumerateFiles(binReleaseDir, "*", SearchOption.AllDirectories))
    {
        if (file.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            continue;

        var rel = Path.GetRelativePath(binReleaseDir, file).Replace('\\', '/');
        archive.CreateEntryFromFile(file, rel, CompressionLevel.Optimal);
    }
}

static async Task<bool> GitRemoteHasTagAsync(string repoRoot, string tag)
{
    using var p = new Process
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = repoRoot,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        },
    };
    p.StartInfo.ArgumentList.Add("ls-remote");
    p.StartInfo.ArgumentList.Add("--tags");
    p.StartInfo.ArgumentList.Add("origin");
    p.StartInfo.ArgumentList.Add(tag);
    p.Start();
    var stdout = await p.StandardOutput.ReadToEndAsync();
    _ = await p.StandardError.ReadToEndAsync();
    await p.WaitForExitAsync();
    return p.ExitCode == 0 && stdout.Trim().Length > 0;
}

static async Task<bool> GitLocalHasTagAsync(string repoRoot, string tag)
{
    var code = await RunAsync("git", repoRoot, "rev-parse", "-q", "--verify", $"refs/tags/{tag}");
    return code == 0;
}

static async Task<int> GhReleaseCreateAsync(string repoRoot, string tag, string zipPath)
{
    var notes = $"Release {tag}.";
    var title = $"PodracingTracker {tag}";
    return await RunAsync(
        "gh",
        repoRoot,
        "release",
        "create",
        tag,
        zipPath,
        "--title",
        title,
        "--notes",
        notes);
}

static async Task<int> GhReleaseUploadAsync(string repoRoot, string tag, string zipPath) =>
    await RunAsync("gh", repoRoot, "release", "upload", tag, zipPath, "--clobber");

var bumpOption = new Option<string?>("--bump")
{
    Description = "patch | minor | major (increment manifest version).",
};
var versionOption = new Option<string?>("--version")
{
    Description = "Set manifest version explicitly (major.minor.patch).",
};
var buildOption = new Option<string?>("--build")
{
    Description = "debug (build only) or release (build + zip TheIterator.PodracingTracker.zip).",
};
var pushOption = new Option<bool>("--push") { Description = "Create/update GitHub release for the manifest version." };

var release = new Command("release", "Bump manifest version, build the mod, package zip, and/or push to GitHub.")
{
    bumpOption,
    versionOption,
    buildOption,
    pushOption,
};

release.SetHandler(
    async (string? bump, string? explicitVersion, string? build, bool push) =>
    {
        if (bump is not null && explicitVersion is not null)
        {
            Console.Error.WriteLine("Use either --bump or --version, not both.");
            Environment.ExitCode = 1;
            return;
        }

        if (bump is null && explicitVersion is null && build is null && !push)
        {
            Console.Error.WriteLine("Specify at least one of --bump, --version, --build, or --push.");
            Environment.ExitCode = 1;
            return;
        }

        var repoRoot = FindRepoRoot();
        var manifestPath = ManifestPath(repoRoot);
        var modProject = ModProjectPath(repoRoot);

        try
        {
            if (explicitVersion is not null)
            {
                var current = await ReadManifestVersionAsync(manifestPath);
                var next = explicitVersion.Trim();
                _ = ParseSemver3(next);
                await WriteManifestVersionAsync(manifestPath, next);
                Console.WriteLine($"manifest version: {current} -> {next}");
            }
            else if (bump is not null)
            {
                var current = await ReadManifestVersionAsync(manifestPath);
                var next = BumpVersion(current, bump);
                await WriteManifestVersionAsync(manifestPath, next);
                Console.WriteLine($"manifest version: {current} -> {next}");
            }

            if (build is not null)
            {
                var cfg = build.Trim().ToLowerInvariant();
                if (cfg is not ("debug" or "release"))
                {
                    Console.Error.WriteLine("--build must be debug or release.");
                    Environment.ExitCode = 1;
                    return;
                }

                var configuration = char.ToUpperInvariant(cfg[0]) + cfg[1..];
                var code = await RunAsync("dotnet", repoRoot, "build", modProject, "-c", configuration);
                if (code != 0)
                {
                    Environment.ExitCode = code;
                    return;
                }

                if (cfg == "release")
                {
                    var binRelease = BinDir(repoRoot, "Release");
                    var zip = ZipPath(repoRoot);
                    CreateModZip(binRelease, zip);
                    Console.WriteLine($"Packaged: {zip}");
                }
            }

            if (push)
            {
                var version = await ReadManifestVersionAsync(manifestPath);
                _ = ParseSemver3(version);
                var tag = $"v{version}";
                var zip = ZipPath(repoRoot);
                if (!File.Exists(zip))
                {
                    Console.Error.WriteLine(
                        $"Missing zip for push: {zip}. Run with --build release (same invocation or earlier) to create it.");
                    Environment.ExitCode = 1;
                    return;
                }

                var ghCheck = await RunAsync("gh", repoRoot, "auth", "status");
                if (ghCheck != 0)
                {
                    Environment.ExitCode = ghCheck;
                    return;
                }

                var hasRemoteTag = await GitRemoteHasTagAsync(repoRoot, tag);
                if (!hasRemoteTag)
                {
                    if (!await GitLocalHasTagAsync(repoRoot, tag))
                    {
                        var tCode = await RunAsync("git", repoRoot, "tag", "-a", tag, "-m", tag);
                        if (tCode != 0)
                        {
                            Environment.ExitCode = tCode;
                            return;
                        }
                    }

                    var pCode = await RunAsync("git", repoRoot, "push", "origin", tag);
                    if (pCode != 0)
                    {
                        Environment.ExitCode = pCode;
                        return;
                    }
                }

                var createCode = await GhReleaseCreateAsync(repoRoot, tag, zip);
                if (createCode == 0)
                {
                    Console.WriteLine($"GitHub release ready: {tag}");
                    return;
                }

                var uploadCode = await GhReleaseUploadAsync(repoRoot, tag, zip);
                Environment.ExitCode = uploadCode != 0 ? uploadCode : 0;
                if (uploadCode == 0)
                    Console.WriteLine($"GitHub release asset updated: {tag}");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            Environment.ExitCode = 1;
        }
    },
    bumpOption,
    versionOption,
    buildOption,
    pushOption);

var root = new RootCommand("Podracing Tracker workspace tooling.");
root.AddCommand(release);

return await root.InvokeAsync(args);
