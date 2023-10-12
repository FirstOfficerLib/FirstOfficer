﻿using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FirstOfficer.Generator
{
    internal class DebugGenerator
    {
        internal static void AttachDebugger()
        {
            if (Debugger.IsAttached)
                return;

            TryAttachDebugger();

            // wait for debugger to be attached (up to 30s)
            for (var i = 0; i < 30 && !Debugger.IsAttached; i++)
            {
                Thread.Sleep(1000);
            }
        }

        private static void TryAttachDebugger()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Debugger.Launch();
                    return;
                }

                // on other operating systems we currently only support rider (make sure "Generate Shell Scripts" is enabled in the jetbrains toolbox app
                // and the generated scripts are in the path)
                Process.Start("rider", $"attach-to-process {Process.GetCurrentProcess().Id} \"{FindSolutionFile()}\"");
            }
            catch (Exception)
            {
                // ignore exceptions if the debugger couldn't be attached
            }
        }

        [SuppressMessage(
            "MicrosoftCodeAnalysisCorrectness",
            "RS1035:Do not use APIs banned for analyzers",
            Justification = "Only used here to launch the debugger correctly and not in included the release build"
        )]
        private static string FindSolutionFile([CallerFilePath] string callerFile = null)
        {
            var dir = Path.GetDirectoryName(callerFile) ??
                      throw new InvalidOperationException("could not resolve solution directory");
            do
            {
                var slnFiles = Directory.GetFiles(dir, "*.sln", SearchOption.TopDirectoryOnly);
                if (slnFiles.Length == 1)
                    return slnFiles[0];

                dir = Path.GetDirectoryName(dir);
            } while (dir != null);

            throw new InvalidOperationException("Could not find solution");
        }
    }

}