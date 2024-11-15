using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace AlexaController.Helpers
{
    static class SteamHelper
    {


        public static void IniciarSteam()
        {
            Process.Start("steam://run");
        }


        public static void CerrarSteam()
        {

            foreach (var process in Process.GetProcessesByName("steam"))
            {
                ProcesosHelper.KillProcessAndChildren(process.Id);
            }

        }

        public  static void ReiniciarSteam()
        {

            foreach (var process in Process.GetProcessesByName("steam"))
            {
                ProcesosHelper.KillProcessAndChildren(process.Id);
            }
            Thread.Sleep(1000); // Espera un segundo antes de reiniciar
            Process.Start("steam://run");
        }
    }
}
