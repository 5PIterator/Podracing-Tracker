using System;
using System.IO;
using System.Reflection;

namespace PodracingTracker;

internal static class ModContentPaths
{
    private static readonly string ModDirectory =
        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
        ?? throw new InvalidDataException("Mod assembly path has no directory.");

    internal static string RulesDirectory => Path.Combine(ModDirectory, "Content");

    internal static string RuleFile(string fileName) => Path.Combine(RulesDirectory, fileName);
}
