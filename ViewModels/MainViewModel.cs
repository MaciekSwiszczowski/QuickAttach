using System.Collections.ObjectModel;
using System.Diagnostics;
using Polly;
using Polly.Retry;
using QuickAttach.Services;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;
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

    public MainViewModel()
    {
        // Load settings

        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        const string folder = @"C:\_work\WorkstationSoftware\build\bin\x64\Debug\net7.0-windows";

        Projects.Add(new Project("ISA", Path.Combine(folder, "InstrumentSimApp.exe"), Color.FromArgb(155, 62, 70, 206)));
        Projects.Add(new Project("MDA", Path.Combine(folder, "ModelDevApp.exe"), Color.FromArgb(155, 238, 26, 33)));
        Projects.Add(new Project("OGA", Path.Combine(folder, "OperatorGuiApp.exe"),
            Color.FromArgb(155, 157, 216, 230)));
        Projects.Add(new Project("PDA", Path.Combine(folder, "ProxyDevApp.exe"), Color.FromArgb(155, 255, 126, 37)));
        Projects.Add(new Project("GDA", Path.Combine(folder, "GuiDevApp.exe"), Color.FromArgb(155, 34, 177, 74)));
        Projects.Add(new Project("CCE", Path.Combine(folder, "CatheterCatalogEditorApp.exe"),
            Color.FromArgb(255, 66, 157, 158)));

        _processEndRetryPolicy = Policy
            .HandleResult<bool>(hasExited => !hasExited)
            .WaitAndRetry(5, static retryAttempt => TimeSpan.FromMilliseconds(50 * retryAttempt));
    }

    public void Stop()
    {
        OnProcessExited(this,  EventArgs.Empty);
    }

    public void RestartAll()
    {
        CanRunAndAttach = true;

        Stop();
        RunAndAttach();
    }

    public void RunAndAttach()
    {
        if ( !CanRunAndAttach)
        {
            return;
        }

        _dispatcherQueue.TryEnqueue(() => CanRunAndAttach = false);


        if ( !Projects.Any(static i => i.Run))
        {
            _dispatcherQueue.TryEnqueue(() => CanRunAndAttach = true);
            return;
        }

        Task.Run(() =>
        {
            var attacher = new VisualStudioAttacher("AllApps");
            if (!attacher.Build())
            {
                _dispatcherQueue.TryEnqueue(() => CanRunAndAttach = true);
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
                    WorkingDirectory = Path.GetDirectoryName(project.Path)
                };

                process.StartInfo = startInfo;
                process.EnableRaisingEvents = true;
                process.Start();
                process.Exited += OnProcessExited;
            }

            var projectsToStart = Projects
                .Where(static i => i is { Attach: true, Run: true })
                .Select(static i => Path.GetFileName(i.Path));

            attacher.Attach(projectsToStart);


            foreach (var process in _processes)
            {
                process.WaitForInputIdle();

                var waitLimit = 25; 
                var currentTry = 0;

                while (process.MainWindowHandle == IntPtr.Zero && currentTry < waitLimit)
                {
                    Thread.Sleep(100); 
                    process.Refresh();
                    currentTry++;
                }

                if (process.MainWindowHandle != IntPtr.Zero)
                {
                    var windowManager = new WindowManager();
                    var windowHandles = windowManager.GetRootWindowsOfProcess(process.Id);
                    windowManager.SetWindowPositions(windowHandles);
                }
            }
        });
    }

    private void OnProcessExited(object? sender, EventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() => CanRunAndAttach = true);
        
        foreach (var process in _processes)
        {
            process.Exited -= OnProcessExited;

            try
            {
                if (process.HasExited)
                {
                    continue;
                }

                process.CloseMainWindow();

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

        var attacher = new VisualStudioAttacher("AllApps");
        attacher.TerminateDebuggingSession();
    }

    private bool _canRunAndAttach = true;
    private readonly List<Process> _processes = new();

    private ObservableCollection<Project> _projects = new();
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly RetryPolicy<bool> _processEndRetryPolicy;
}