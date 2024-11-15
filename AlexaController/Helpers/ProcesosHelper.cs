using System.Diagnostics;

namespace AlexaController.Helpers
{
    internal static class ProcesosHelper
    {
        public static void CerrarRetroArch()
        {
            foreach (var process in Process.GetProcessesByName("retroarch"))
            {
                KillProcessAndChildren(process.Id);
            }
        }

        public static void KillProcessAndChildren(int pid)
        {
            var process = Process.GetProcessById(pid);
            if (process == null) return;

            foreach (var child in Process.GetProcesses()
                        .Where(p => GetParentProcessId(p) == pid))
            {
                KillProcessAndChildren(child.Id);
            }
            process.Kill();
            process.WaitForExit(); // Asegura que el proceso finalice antes de continuar
        }

        private static int GetParentProcessId(Process process)
        {
            try
            {
                using (var mo = new System.Management.ManagementObject(
                    $"win32_process.handle='{process.Id}'"))
                {
                    mo.Get();
                    return Convert.ToInt32(mo["ParentProcessId"]);
                }
            }
            catch
            {
                return 0;
            }
        }
    }
}