﻿using System.Collections.ObjectModel;
using System.Diagnostics;
using QuickAttach.Services;
using WindowManager = QuickAttach.Services.WindowManager;

namespace QuickAttach.ViewModels;

public class MainViewModel : ObservableRecipient
{
    public ObservableCollection<Project> Projects
    {
        get => _projects;
        set => SetProperty(ref _projects, value);
    }

    public bool CanRunAndAttach
    {
        get => _canRunAndAttach;
        set => SetProperty(ref _canRunAndAttach, value);
    }

    public XamlRoot? Root
    {
        get;
        set;
    }

    public MainViewModel()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        const string folder = @"C:\_work\WorkstationSoftware\build\bin\x64\Debug\net7.0-windows";
        var projectDetails = GetProjectDetails();
        foreach (var detail in projectDetails)
        {
            Projects.Add(CreateProject(detail, folder));
        }
    }

    private Project CreateProject((string Name, string Executable, Color ProjectColor) detail, string folder) =>
        new(detail.Name, Path.Combine(folder, $"{detail.Executable}.exe"), detail.ProjectColor);

    private List<(string Name, string Executable, Color ProjectColor)> GetProjectDetails() =>
        new()
        {
            ("ISA", "InstrumentSimApp", Color.FromArgb(155, 62, 70, 206)),
            ("MDA", "ModelDevApp", Color.FromArgb(155, 238, 26, 33)),
            ("OGA", "OperatorGuiApp", Color.FromArgb(155, 157, 216, 230)),
            ("PDA", "ProxyDevApp", Color.FromArgb(155, 255, 126, 37)),
            ("GDA", "GuiDevApp", Color.FromArgb(155, 34, 177, 74)),
            ("CCE", "CatheterCatalogEditorApp", Color.FromArgb(255, 66, 157, 158))
        };


    public void Stop()
    {
        StopAllProcesses();
        TerminateDebugSession();
    }

    public void RestartAll()
    {
        CanRunAndAttach = true;

        Stop();
        RunAndAttach();
    }

    [DebuggerStepThrough]
    public void RunAndAttach()
    {
        if (!CanRunAndAttach)
        {
            return;
        }

        _dispatcherQueue.TryEnqueue(() => CanRunAndAttach = false);


        if (!Projects.Any(static i => i.Run))
        {
            _dispatcherQueue.TryEnqueue(() => CanRunAndAttach = true);
            return;
        }

        Task.Run(() =>
        {
            _attacher?.Dispose();
            _attacher = new VisualStudioAttacher("AllApps")
            {
                OnStopDebugging = StopAllProcesses
            };

            if (!_attacher.Build())
            {
                _dispatcherQueue.TryEnqueue(() => CanRunAndAttach = true);
                _attacher.OnStopDebugging();
                _attacher.Dispose();
                return;
            }

            _processes.Clear();

            foreach (var project in Projects)
            {
                if (!project.Run)
                {
                    continue;
                }

                var process = new Process();
                _processes.Add(process);

                var startInfo = new ProcessStartInfo
                {
                    FileName = project.Path,
                    WorkingDirectory = Path.GetDirectoryName(project.Path),
                    UseShellExecute = false,
                    CreateNoWindow = false,
                    WindowStyle = ProcessWindowStyle.Normal
                };

                process.StartInfo = startInfo;
                process.EnableRaisingEvents = true;
                process.Start();
                process.Exited += OnProcessExited;
            }

            var projectsToStart = Projects
                .Where(static i => i is {Attach: true, Run: true})
                .Select(static i => Path.GetFileName(i.Path));

            _attacher.Attach(projectsToStart);

            foreach (var process in _processes)
            {
                process.WaitForInputIdle();

                var handle = _mainWindowHandleRetryPolicy.Execute(() =>
                {
                    try
                    {
                        return process.MainWindowHandle;
                    }
                    catch (InvalidOperationException)
                    {
                        return IntPtr.Zero;
                    }
                });

                if (handle == IntPtr.Zero)
                {
                    continue;
                }

                var windowManager = new WindowManager();
                var windowHandles = windowManager.GetRootWindowsOfProcess(process.Id);
                windowManager.SetWindowPositions(windowHandles);
            }
        });
    }

    private void OnProcessExited(object? sender, EventArgs e) => Stop();

    private void TerminateDebugSession()
    {
        _attacher?.TerminateDebuggingSession();
        _attacher?.Dispose();
    }

    private void StopAllProcesses()
    {
        _dispatcherQueue.TryEnqueue(() => CanRunAndAttach = true);

        foreach (var process in _processes)
        {
            process.Exited -= OnProcessExited;

            try
            {
                process.Refresh();

                if (process.HasExited)
                {
                    continue;
                }

                process.CloseMainWindow();

                process.Refresh();

                _processEndRetryPolicy.Execute(() => process.HasExited);

                if (!process.HasExited)
                {
                    process.Kill();
                }
            }
            finally
            {
                process.Dispose();
            }
        }
    }

    private readonly DispatcherQueue _dispatcherQueue;

    private readonly RetryPolicy<IntPtr> _mainWindowHandleRetryPolicy = Policy
        .Handle<InvalidOperationException>()
        .OrResult<IntPtr>(static handle => handle == IntPtr.Zero)
        .WaitAndRetry(5, static retryAttempt => TimeSpan.FromMilliseconds(150 + 50 * retryAttempt));

    private readonly RetryPolicy<bool> _processEndRetryPolicy = Policy
        .HandleResult<bool>(static hasExited => !hasExited)
        .WaitAndRetry(10, static retryAttempt => TimeSpan.FromMilliseconds(50 * retryAttempt));
    private readonly List<Process> _processes = new();
    private VisualStudioAttacher? _attacher;
    private bool _canRunAndAttach = true;
    private ObservableCollection<Project> _projects = new();
}