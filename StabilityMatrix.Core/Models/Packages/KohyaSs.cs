﻿using System.Text.RegularExpressions;
using Python.Runtime;
using StabilityMatrix.Core.Attributes;
using StabilityMatrix.Core.Extensions;
using StabilityMatrix.Core.Helper;
using StabilityMatrix.Core.Helper.Cache;
using StabilityMatrix.Core.Helper.HardwareInfo;
using StabilityMatrix.Core.Models.FileInterfaces;
using StabilityMatrix.Core.Models.Progress;
using StabilityMatrix.Core.Processes;
using StabilityMatrix.Core.Python;
using StabilityMatrix.Core.Services;

namespace StabilityMatrix.Core.Models.Packages;

[Singleton(typeof(BasePackage))]
public class KohyaSs(
    IGithubApiCache githubApi,
    ISettingsManager settingsManager,
    IDownloadService downloadService,
    IPrerequisiteHelper prerequisiteHelper,
    IPyRunner runner
) : BaseGitPackage(githubApi, settingsManager, downloadService, prerequisiteHelper)
{
    public override string Name => "kohya_ss";
    public override string DisplayName { get; set; } = "kohya_ss";
    public override string Author => "bmaltais";
    public override string Blurb => "A Windows-focused Gradio GUI for Kohya's Stable Diffusion trainers";
    public override string LicenseType => "Apache-2.0";
    public override string LicenseUrl => "https://github.com/bmaltais/kohya_ss/blob/master/LICENSE.md";
    public override string LaunchCommand => "kohya_gui.py";

    public override Uri PreviewImageUri =>
        new(
            "https://camo.githubusercontent.com/5154eea62c113d5c04393e51a0d0f76ef25a723aad29d256dcc85ead1961cd41/68747470733a2f2f696d672e796f75747562652e636f6d2f76692f6b35696d713031757655592f302e6a7067"
        );
    public override string OutputFolderName => string.Empty;

    public override bool IsCompatible => HardwareHelper.HasNvidiaGpu();

    public override TorchVersion GetRecommendedTorchVersion() => TorchVersion.Cuda;

    public override string Disclaimer =>
        "Nvidia GPU with at least 8GB VRAM is recommended. May be unstable on Linux.";

    public override PackageDifficulty InstallerSortOrder => PackageDifficulty.UltraNightmare;
    public override PackageType PackageType => PackageType.SdTraining;
    public override bool OfferInOneClickInstaller => false;
    public override SharedFolderMethod RecommendedSharedFolderMethod => SharedFolderMethod.None;
    public override IEnumerable<TorchVersion> AvailableTorchVersions => [TorchVersion.Cuda];
    public override IEnumerable<SharedFolderMethod> AvailableSharedFolderMethods =>
        new[] { SharedFolderMethod.None };
    public override IEnumerable<PackagePrerequisite> Prerequisites =>
        base.Prerequisites.Concat([PackagePrerequisite.Tkinter]);

    public override List<LaunchOptionDefinition> LaunchOptions =>
        [
            new LaunchOptionDefinition
            {
                Name = "Listen Address",
                Type = LaunchOptionType.String,
                DefaultValue = "127.0.0.1",
                Options = ["--listen"]
            },
            new LaunchOptionDefinition
            {
                Name = "Port",
                Type = LaunchOptionType.String,
                Options = ["--port"]
            },
            new LaunchOptionDefinition
            {
                Name = "Username",
                Type = LaunchOptionType.String,
                Options = ["--username"]
            },
            new LaunchOptionDefinition
            {
                Name = "Password",
                Type = LaunchOptionType.String,
                Options = ["--password"]
            },
            new LaunchOptionDefinition
            {
                Name = "Auto-Launch Browser",
                Type = LaunchOptionType.Bool,
                Options = ["--inbrowser"]
            },
            new LaunchOptionDefinition
            {
                Name = "Share",
                Type = LaunchOptionType.Bool,
                Options = ["--share"]
            },
            new LaunchOptionDefinition
            {
                Name = "Headless",
                Type = LaunchOptionType.Bool,
                Options = ["--headless"]
            },
            new LaunchOptionDefinition
            {
                Name = "Language",
                Type = LaunchOptionType.String,
                Options = ["--language"]
            },
            LaunchOptionDefinition.Extras
        ];

    public override async Task InstallPackage(
        string installLocation,
        TorchVersion torchVersion,
        SharedFolderMethod selectedSharedFolderMethod,
        DownloadPackageVersionOptions versionOptions,
        IProgress<ProgressReport>? progress = null,
        Action<ProcessOutput>? onConsoleOutput = null
    )
    {
        progress?.Report(new ProgressReport(-1f, "Updating submodules", isIndeterminate: true));
        await PrerequisiteHelper
            .RunGit(
                ["submodule", "update", "--init", "--recursive", "--quiet"],
                onConsoleOutput,
                installLocation
            )
            .ConfigureAwait(false);

        progress?.Report(new ProgressReport(-1f, "Setting up venv", isIndeterminate: true));

        // Setup venv
        await using var venvRunner = new PyVenvRunner(Path.Combine(installLocation, "venv"));
        venvRunner.WorkingDirectory = installLocation;
        venvRunner.EnvironmentVariables = settingsManager.Settings.EnvironmentVariables;

        await venvRunner.Setup(true, onConsoleOutput).ConfigureAwait(false);

        // Extra dep needed before running setup since v23.0.x
        await venvRunner.PipInstall(["rich", "packaging"]).ConfigureAwait(false);

        if (Compat.IsWindows)
        {
            // Install
            await venvRunner
                .CustomInstall("setup/setup_windows.py", onConsoleOutput)
                .ConfigureAwait(false);
        }
        else if (Compat.IsLinux)
        {
            await venvRunner
                .CustomInstall("setup/setup_linux.py", onConsoleOutput)
                .ConfigureAwait(false);
        }
    }

    public override async Task RunPackage(
        string installedPackagePath,
        string command,
        string arguments,
        Action<ProcessOutput>? onConsoleOutput
    )
    {
        var venvRunner = await SetupVenvPure(installedPackagePath).ConfigureAwait(false);

        void HandleConsoleOutput(ProcessOutput s)
        {
            onConsoleOutput?.Invoke(s);

            if (!s.Text.Contains("Running on", StringComparison.OrdinalIgnoreCase))
                return;

            var regex = new Regex(@"(https?:\/\/)([^:\s]+):(\d+)");
            var match = regex.Match(s.Text);
            if (!match.Success)
                return;

            WebUrl = match.Value;
            OnStartupComplete(WebUrl);
        }

        var args = $"\"{Path.Combine(installedPackagePath, command)}\" {arguments}";

        venvRunner.RunDetached(args.TrimEnd(), HandleConsoleOutput, OnExit);
    }

    public override Dictionary<SharedFolderType, IReadOnlyList<string>>? SharedFolders { get; }
    public override Dictionary<SharedOutputType, IReadOnlyList<string>>? SharedOutputFolders { get; }

    public override string MainBranch => "master";
}
