namespace AlexaController.Helpers
{
    using System;
    using Microsoft.Win32;

    class GameModeManager
    {
        private const string GameBarRegistryPath = @"Software\Microsoft\GameBar";
        private const string GameModeKeyName = "AllowAutoGameMode";
        private static bool gameModeActivatedByThisProgram = false;

        // Método para activar siempre el Modo de Juego
        public static void EnableGameMode()
        {
            Console.WriteLine("Activando Modo de Juego...");
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(GameBarRegistryPath, true)
                                ?? Registry.CurrentUser.CreateSubKey(GameBarRegistryPath);

                key.SetValue(GameModeKeyName, 1);
                gameModeActivatedByThisProgram = true; // Indica que este programa lo activó
                Console.WriteLine("Modo de Juego activado correctamente.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al activar el Modo de Juego: {ex.Message}");
            }
        }

        // Método para desactivar el Modo de Juego solo si fue activado por este programa
        public static void DisableGameMode()
        {
            if (!gameModeActivatedByThisProgram)
            {
                Console.WriteLine("El Modo de Juego no fue activado por este programa. No se realizará ninguna acción.");
                return;
            }

            Console.WriteLine("Desactivando Modo de Juego...");
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(GameBarRegistryPath, true);
                if (key == null)
                {
                    Console.WriteLine("Clave de registro no encontrada. El Modo de Juego ya está desactivado.");
                    return;
                }

                key.SetValue(GameModeKeyName, 0);
                gameModeActivatedByThisProgram = false; // Resetea el indicador
                Console.WriteLine("Modo de Juego desactivado correctamente.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al desactivar el Modo de Juego: {ex.Message}");
            }
        }
    }



}
