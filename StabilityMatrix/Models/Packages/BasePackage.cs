﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Octokit;

namespace StabilityMatrix.Models.Packages;

public abstract class BasePackage
{
    public abstract string Name { get; }
    public abstract string DisplayName { get; set; }
    public abstract string Author { get; }
    public abstract string GithubUrl { get; }
    public abstract string LaunchCommand { get; }
    public virtual bool ShouldIgnoreReleases => false;
    public virtual bool UpdateAvailable { get; set; }
    public abstract Task<string?> DownloadPackage(string version, bool isCommitHash, bool isUpdate = false);
    public abstract Task InstallPackage(bool isUpdate = false);
    public abstract Task RunPackage(string installedPackagePath, string arguments);
    public abstract Task Shutdown();
    public abstract Task<bool> CheckForUpdates(string installedPackageName);
    public abstract Task<string> Update(InstalledPackage installedPackage);
    public abstract Task<IOrderedEnumerable<Release>> GetReleaseTags();

    public abstract List<LaunchOptionDefinition> LaunchOptions { get; }
    public virtual string? ExtraLaunchArguments { get; set; } = null;
    
    public abstract Task<string> GetLatestVersion();
    public abstract Task<IEnumerable<PackageVersion>> GetAllVersions(bool isReleaseMode = true);
    public abstract Task<IReadOnlyList<GitHubCommit>?> GetAllCommits(string branch, int page = 1, int perPage = 10);
    public abstract Task<IReadOnlyList<Branch>> GetAllBranches();
    public abstract Task<IOrderedEnumerable<Release>> GetAllReleases();

    public abstract string DownloadLocation { get; }
    public abstract string InstallLocation { get; set; }

    public event EventHandler<int>? DownloadProgressChanged;
    public event EventHandler<string>? DownloadComplete;
    public event EventHandler<int>? InstallProgressChanged;
    public event EventHandler<string>? InstallComplete;
    public event EventHandler<int>? UpdateProgressChanged;
    public event EventHandler<string>? UpdateComplete;
    public event EventHandler<string>? ConsoleOutput;
    public event EventHandler<int>? Exited;
    public event EventHandler<string>? StartupComplete;

    public void OnDownloadProgressChanged(int progress) => DownloadProgressChanged?.Invoke(this, progress);
    public void OnDownloadComplete(string path) => DownloadComplete?.Invoke(this, path);
    public void OnInstallProgressChanged(int progress) => InstallProgressChanged?.Invoke(this, progress);
    public void OnInstallComplete(string path) => InstallComplete?.Invoke(this, path);
    public void OnConsoleOutput(string output) => ConsoleOutput?.Invoke(this, output);
    public void OnExit(int exitCode) => Exited?.Invoke(this, exitCode);
    public void OnStartupComplete(string url) => StartupComplete?.Invoke(this, url);
    public void OnUpdateProgressChanged(int progress) => UpdateProgressChanged?.Invoke(this, progress);
    public void OnUpdateComplete(string path) => UpdateComplete?.Invoke(this, path);
    
    

    public string ByAuthor => $"By {Author}";
}