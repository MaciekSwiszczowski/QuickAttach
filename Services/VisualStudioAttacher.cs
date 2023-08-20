﻿using System.Management;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio.OLE.Interop;

namespace QuickAttach.Services;

public class VisualStudioAttacher
{
    private readonly string _targetSolutionName;
    private readonly DTE? _dte;

    public VisualStudioAttacher(string targetSolutionName)
    {
        _targetSolutionName = targetSolutionName;
        _dte = GetDte();
    }

    private const string VisualStudioProcessName = "DevEnv";

    public bool Build()
    {
        _dte?.Solution.SolutionBuild.Build(true);

        return _dte?.Solution.SolutionBuild.LastBuildInfo == 0;
    }

    private DTE? GetDte()
    {
        var searcher =
            new ManagementObjectSearcher(
                $"SELECT ProcessId FROM Win32_Process WHERE Name = '{VisualStudioProcessName}.exe'");
        var collection = searcher.Get();

        foreach (var item in collection)
        {
            var processId = Convert.ToInt32(item["ProcessId"]);
            var dte = GetDteForSolution(processId, _targetSolutionName);
            if (dte != null)
            {
                return dte;
            }
        }

        return null;
    }

    public void Attach(IEnumerable<string> processNames)
    {
        if (_dte != null)
        {
            AttachAll(processNames, _dte);
        }
    }

    private void AttachAll(IEnumerable<string> processNames, DTE dte)
    {
        foreach (var processName in processNames)
        {
            AttachToProcess(dte, processName);
        }
    }

    private DTE? GetDteForSolution(int processId, string targetSolutionName)
    {
        var dte = GetDte(processId);
        if (dte == null)
        {
            return null;
        }

        var openedSolutionPath = dte.Solution.FullName;
        return openedSolutionPath.Contains(targetSolutionName, StringComparison.OrdinalIgnoreCase) ? dte : null;
    }

    private DTE? GetDte(int processId)
    {
        GetRunningObjectTable(0, out var rot);
        rot.EnumRunning(out var enumMoniker);
        enumMoniker.Reset();
        var moniker = new IMoniker[1];
        while (enumMoniker.Next(1, moniker, out _) == 0)
        {
            CreateBindCtx(0, out var bindCtx);
            moniker[0].GetDisplayName(bindCtx, null, out var displayName);
            if (!displayName.StartsWith($"!VisualStudio.DTE.17.0:{processId}"))
            {
                continue;
            }

            rot.GetObject(moniker[0], out var comObject);
            return comObject as DTE;
        }

        return null;
    }

    private void AttachToProcess(DTE dte, string processName)
    {
        var process = dte.Debugger.LocalProcesses.Cast<Process>().FirstOrDefault(p => p.Name.Contains(processName, StringComparison.OrdinalIgnoreCase));

        if (process != null)
        {
            process.Attach();
            Console.WriteLine($@"Attached to {processName}");
        }
        else
        {
            Console.WriteLine($@"Process {processName} not found.");
        }
    }

    [DllImport("ole32.dll")]
    private static extern int GetRunningObjectTable(uint reserved, out IRunningObjectTable prot);

    [DllImport("ole32.dll")]
    private static extern int CreateBindCtx(uint reserved, out IBindCtx ppbc);
}