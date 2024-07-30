using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
using Newtonsoft.Json;

namespace Build;

public static class Program
{
    public static int Main(string[] args)
    {
        return new CakeHost()
            .UseContext<BuildContext>()
            // .UseLifetime<BuildLifetime>()
            .Run(args);
    }
}

public class BuildContext : FrostingContext
{
    public string DotNetBuildConfig { get; set; }
    public const string SlnFile = "../Flow.Launcher.Plugin.ClipboardPlus.sln";
    public Lazy<SolutionParserResult> DefaultSln { get; set; }
    public const string DeployFramework = "net7.0-windows";
    public string PublishDir = ".dist";
    public string PublishVersion = "";
    public string BuildFor = "win-x64"; // win-x64 win-x86

    public BuildContext(ICakeContext context)
        : base(context)
    {
        DefaultSln = new Lazy<SolutionParserResult>(() => context.ParseSolution(SlnFile));
        DotNetBuildConfig = context.Argument("configuration", "Release");
    }
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

        // var builder = context.DefaultSln.Value.Projects.First(p => p.Name.EndsWith("Build"));
        // var midDir = builder.Path.GetDirectory().Combine(new DirectoryPath("bin/Publish"));
        // if (context.DirectoryExists(midDir))
        //     context.DeleteDirectory(midDir, new DeleteDirectorySettings { Recursive = true, Force = true });
        // context.CreateDirectory(midDir);

        // context.CopyDirectory(srcDir, midDir);

        var ptn =
            // Core
            @"ClipboardPlus\.dll|ClipboardPlus.+\.dll|"
            // Image
            + @".+\.png|"
            // Manifest
            + @"plugin\.json|"
            // Nuget
            + @"Dapper\.dll|"
            + @"H\.InputSimulator\.dll|"
            + @"FluentIcons\.Common\.dll|FluentIcons\.WPF\.dll|"
            + @"SQLitePCLRaw.+\.dll|Microsoft.+(S|s)qlite\.dll";
        var files = context.GetFiles($"{srcDir}/**/*");
        FilePath? versionFile = null;
        foreach (var f in files)
        {
            var fStr = f.ToString();
            var fName = f.GetFilename().ToString();
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
                versionFile = f;
            }

            if (fName == "FluentIcons.WPF.dll")
            {
                var match = Regex.IsMatch(fName, ptn);
            }

            if (!Regex.IsMatch(fName, ptn))
            {
                context.DeleteFile(f);
                files.Remove(f);
            }
            else
            {
                context.Information($"Added: {f}");
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
            VersionInfo? versionInfoObj = JsonConvert.DeserializeObject<VersionInfo>(
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
            outputPath: $"{dstDir}/ClipboardPlus-v{context.PublishVersion}.zip",
            filePaths: files,
            level: 9
        );
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

[TaskName("Deploy")]
public class DeployTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var builder = context.DefaultSln.Value.Projects.First(p => p.Name.EndsWith("Build"));
        var distDir = builder.Path
            .GetDirectory()
            .GetParent()
            .Combine(new DirectoryPath(context.PublishDir));
        var files = context.GetFiles($"{distDir}/ClipboardPlus*.zip");

        DateTime t = DateTime.FromFileTime(0);
        FilePath mostRecentFile = files.First();
        foreach (var f in files)
        {
            if (File.GetCreationTime(f.FullPath) <= t)
            {
                continue;
            }

            t = File.GetCreationTime(f.FullPath);
            mostRecentFile = f;
        }

        context.Unzip(
            mostRecentFile,
            new DirectoryPath(
                @"%APPDATA%/FlowLauncher/Plugins" + mostRecentFile.GetFilenameWithoutExtension()
            )
        );
    }
}

public class VersionInfo
{
    public string? ID { get; set; }
    public string? ActionKeyword { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Author { get; set; }
    public string? Version { get; set; }
    public string? Language { get; set; }
    public string? Website { get; set; }
    public string? IcoPath { get; set; }
    public string? ExecuteFileName { get; set; }
}
