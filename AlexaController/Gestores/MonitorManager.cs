namespace AlexaController.Helpers
{
    using System;
    using System.Runtime.InteropServices;

    internal class MonitorConfiguration
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE devMode);

        [DllImport("user32.dll")]
        private static extern int ChangeDisplaySettingsEx(string lpszDeviceName, ref DEVMODE lpDevMode, IntPtr hwnd, uint dwFlags, IntPtr lParam);

        private const int ENUM_CURRENT_SETTINGS = -1;
        private const int CDS_UPDATEREGISTRY = 0x01;
        private const int CDS_NORESET = 0x10000000;
        private const int DISP_CHANGE_SUCCESSFUL = 0;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct DISPLAY_DEVICE
        {
            public int cb;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;

            public int StateFlags;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct DEVMODE
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmDeviceName;

            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public int dmDisplayOrientation;
            public int dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmFormName;

            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;
        }

        public static void ConfigureSingleMonitor()
        {
            Console.WriteLine("Configurando para usar un solo monitor...");
            try
            {
                var displayDevice = new DISPLAY_DEVICE();
                displayDevice.cb = Marshal.SizeOf(displayDevice);

                if (EnumDisplayDevices(null, 0, ref displayDevice, 0))
                {
                    var devMode = new DEVMODE();
                    devMode.dmSize = (short)Marshal.SizeOf(devMode);

                    if (EnumDisplaySettings(displayDevice.DeviceName, ENUM_CURRENT_SETTINGS, ref devMode))
                    {
                        devMode.dmPositionX = 0;
                        devMode.dmPositionY = 0;

                        int result = ChangeDisplaySettingsEx(displayDevice.DeviceName, ref devMode, IntPtr.Zero, CDS_UPDATEREGISTRY, IntPtr.Zero);

                        if (result == DISP_CHANGE_SUCCESSFUL)
                        {
                            Console.WriteLine("Configuración actualizada a un solo monitor.");
                        }
                        else
                        {
                            Console.WriteLine($"Error al configurar el monitor. Código: {result}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        public static void ConfigureDualMonitors()
        {
            Console.WriteLine("Configurando para usar dos monitores...");
            try
            {
                var primaryDevice = new DISPLAY_DEVICE();
                var secondaryDevice = new DISPLAY_DEVICE();
                primaryDevice.cb = Marshal.SizeOf(primaryDevice);
                secondaryDevice.cb = Marshal.SizeOf(secondaryDevice);

                if (EnumDisplayDevices(null, 0, ref primaryDevice, 0) &&
                    EnumDisplayDevices(null, 1, ref secondaryDevice, 0))
                {
                    var primaryMode = new DEVMODE();
                    var secondaryMode = new DEVMODE();
                    primaryMode.dmSize = (short)Marshal.SizeOf(primaryMode);
                    secondaryMode.dmSize = (short)Marshal.SizeOf(secondaryMode);

                    if (EnumDisplaySettings(primaryDevice.DeviceName, ENUM_CURRENT_SETTINGS, ref primaryMode) &&
                        EnumDisplaySettings(secondaryDevice.DeviceName, ENUM_CURRENT_SETTINGS, ref secondaryMode))
                    {
                        primaryMode.dmPositionX = 0;
                        primaryMode.dmPositionY = 0;

                        secondaryMode.dmPositionX = primaryMode.dmPelsWidth;
                        secondaryMode.dmPositionY = 0;

                        int resultPrimary = ChangeDisplaySettingsEx(primaryDevice.DeviceName, ref primaryMode, IntPtr.Zero, CDS_NORESET, IntPtr.Zero);
                        int resultSecondary = ChangeDisplaySettingsEx(secondaryDevice.DeviceName, ref secondaryMode, IntPtr.Zero, CDS_UPDATEREGISTRY, IntPtr.Zero);

                        if (resultPrimary == DISP_CHANGE_SUCCESSFUL && resultSecondary == DISP_CHANGE_SUCCESSFUL)
                        {
                            Console.WriteLine("Configuración actualizada a dos monitores.");
                        }
                        else
                        {
                            Console.WriteLine($"Error al configurar los monitores. Códigos: Primario: {resultPrimary}, Secundario: {resultSecondary}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}