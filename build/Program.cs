using Cake.Common;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Common.Solution;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Build;
using Cake.Common.Tools.DotNet.Publish;
using Cake.Compression;
using Cake.Core;
using Cake.Core.IO;
using Cake.Frosting;
using Flow.Launcher.Plugin;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Build;

public static class Program
{
    public static int Main(string[] args)
    {
        return new CakeHost()
            .UseContext<BuildContext>()
            .Run(args);
    }
}

public class BuildContext(ICakeContext context) : FrostingContext(context)
{
    public string DotNetBuildConfig { get; set; } = context.Argument("configuration", "Release");
    public const string SlnFile = "../Flow.Launcher.Plugin.ClipboardPlus.sln";
    public Lazy<SolutionParserResult> DefaultSln { get; set; } = new Lazy<SolutionParserResult>(() => context.ParseSolution(SlnFile));
    public const string DeployFramework = "net9.0-windows10.0.19041.0";
    public string PublishDir = "output";
    public string PublishVersion = string.Empty;
    public string BuildFor = "win-x64"; // win-x64 win-x86
}

public class BuildLifetime : FrostingLifetime<BuildContext>
{
    public override void Setup(BuildContext context, ISetupContext info)
    {
        var clean = new CleanTask();
        clean.Run(context);
    }

    public override void Teardown(BuildContext context, ITeardownContext info)
    {
        // ignore
    }
}

[TaskName("Build")]
public sealed class BuildTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var projects = context.DefaultSln.Value.Projects.Where(p => p.Name.EndsWith("ClipboardPlus"));
        var projectPath = projects.First().Path.FullPath;
        context.Information($"Building {projectPath}");
        context.DotNetBuild(
            projectPath,
            new DotNetBuildSettings
            {
                Configuration = context.DotNetBuildConfig,
                Verbosity = DotNetVerbosity.Minimal,
                Framework = BuildContext.DeployFramework,
                NoDependencies = false,
                NoIncremental = true,
            }
        );
    }
}

[TaskName("Publish")]
[IsDependentOn(typeof(BuildTask))]
public class PublishTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var project = context.DefaultSln.Value.Projects.First(p => p.Name.EndsWith("ClipboardPlus"));
        var srcDir = project.Path.GetDirectory().Combine(new DirectoryPath("bin/Publish"));
        var dstDir =
            $"{srcDir.GetParent().GetParent().GetParent().GetParent().FullPath}/{context.PublishDir}";
        context.DotNetPublish(
            project.Path.FullPath,
            new DotNetPublishSettings
            {
                OutputDirectory = srcDir,
                Configuration = context.DotNetBuildConfig,
                Framework = BuildContext.DeployFramework,
                Verbosity = DotNetVerbosity.Minimal,
            }
        );
        context.CreateDirectory(dstDir);

        var ptn =
            // Plugin
            @"Flow.Launcher.Plugin.ClipboardPlus\.dll|Flow.Launcher.Plugin.ClipboardPlus.+\.dll|"
            // Nuget Packages
            + @"Dapper.dll|"
            + @"H.InputSimulator.dll|"
            + @"Flow.Launcher.Localization.Attributes.dll|"
            + @"SQLitePCLRaw.+\.dll|Microsoft.+(S|s)qlite\.dll";
        var files = context.GetFiles($"{srcDir}/**/*");
        FilePath? versionFile = null;
        foreach (var f in files)
        {
            var fStr = f.ToString();
            var fName = f.GetFilename().ToString();
            var fFolder = GetLastFolder(fStr);
            if (fStr == null || fName == null)
            {
                continue;
            }

            if (fStr.EndsWith("e_sqlite3.dll") && !fStr.EndsWith(".e_sqlite3.dll"))
            {
                files.Remove(f);
                continue;
            }

            if (fStr.EndsWith("plugin.json"))
            {
                context.Information($"Added: {f} - {fFolder}");
                versionFile = f;
                continue;
            }

            if (GetLastFolder(fStr) == "Images")
            {
                context.Information($"Added: {f} - {fFolder}");
                continue;
            }

            if (GetLastFolder(fStr) == "Languages")
            {
                context.Information($"Added: {f} - {fFolder}");
                continue;
            }

            if (!Regex.IsMatch(fName, ptn))
            {
                context.DeleteFile(f);
                files.Remove(f);
            }
            else
            {
                context.Information($"Added: {f} - {fFolder}");
            }
        }

        var eSqlite3Path = srcDir
            .Combine($"runtimes")
            .Combine($"{context.BuildFor}")
            .Combine("native")
            .CombineWithFilePath(new FilePath("e_sqlite3.dll"));
        context.CopyFile(eSqlite3Path, srcDir.CombineWithFilePath("e_sqlite3.dll"));
        files.Add(srcDir.CombineWithFilePath("e_sqlite3.dll"));
        context.DeleteDirectory(
            srcDir.Combine("runtimes"),
            new DeleteDirectorySettings() { Recursive = true }
        );

        if (versionFile != null)
        {
            PluginMetadata? versionInfoObj = JsonConvert.DeserializeObject<PluginMetadata>(
                File.ReadAllText(versionFile.ToString()!)
            );
            if (versionInfoObj != null)
            {
                context.PublishVersion = versionInfoObj.Version ?? "0.0.0";
            }
            else
            {
                Console.WriteLine("Get version info from plugin.json failed!");
            }
        }

        context.ZipCompress(
            rootPath: srcDir,
            outputPath: $"{dstDir}/Clipboard+-{context.PublishVersion}.zip",
            filePaths: files,
            level: 9
        );
    }

    private static string GetLastFolder(string path)
    {
        // Get the directory part of the path
        var directoryPath = System.IO.Path.GetDirectoryName(path);

        // Create a DirectoryInfo object for the directory path
        DirectoryInfo directoryInfo = new(directoryPath!);

        // Split the full path into parts
        string[] parts = directoryInfo.FullName.Split(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);

        // Reverse the parts
        Array.Reverse(parts);

        // Check if there are parts available
        if (parts.Length > 0)
        {
            // Return the last non-empty part
            foreach (string part in parts)
            {
                if (!string.IsNullOrEmpty(part))
                {
                    return part;
                }
            }
        }

        // If no valid folder is found, return an empty string
        return string.Empty;
    }
}

[TaskName("Clean")]
public sealed class CleanTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        foreach (var project in context.DefaultSln.Value.Projects)
        {
            context.Information($"Cleaning {project.Path.GetDirectory().FullPath}...");
            context.CleanDirectory(
                $"{project.Path.GetDirectory().FullPath}/bin/{context.DotNetBuildConfig}"
            );
        }
    }
}

[TaskName("Default")]
[IsDependentOn(typeof(CleanTask))]
[IsDependentOn(typeof(BuildTask))]
[IsDependentOn(typeof(PublishTask))]
public class DefaultTask : FrostingTask { }
