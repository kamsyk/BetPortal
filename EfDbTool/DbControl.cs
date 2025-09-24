using System.Configuration;

namespace EfDbTool
{
    internal class DbControl
    {
        private const string ConsoleInfoPrefix = "[INFO]";
        private const string ConsoleErrorPrefix = "[ERROR]";
        private const string ConsoleConfirmationPrefix = "[CONFIRMATION]";

        private const ConsoleKey MigrationKey = ConsoleKey.M;
        private const ConsoleKey UpdateKey = ConsoleKey.U;
        private const ConsoleKey ExitKey = ConsoleKey.E;

        private static Configuration? _config = null;

        public enum ConsoleMessageType
        {
            Info,
            Error,
            Confirmation,
        }

        public static void DisplayConsoleMsg(string consoleMsg, ConsoleMessageType consoleMessageType)
        {
            if (consoleMessageType == ConsoleMessageType.Error)
            {
                Console.WriteLine($"{DbControl.ConsoleErrorPrefix} {consoleMsg}");
            }
            else if (consoleMessageType == ConsoleMessageType.Confirmation)
            {
                Console.WriteLine($"{DbControl.ConsoleConfirmationPrefix} {consoleMsg}");
            }
            else
            {
                Console.WriteLine($"{DbControl.ConsoleInfoPrefix} {consoleMsg}");
            }
        }

        public static void Run(string environmentName, bool isSilentMode = false)
        {
            _config = GetConfigFile(environmentName);
            var selectedItemKey = ShowMenu(environmentName);

            switch (selectedItemKey)
            {
                case "M":
                    ////Migration.Run(_config, isSilentMode);
                    break;
                case "U":
                    ////Update.Run(_config, isSilentMode);
                    break;
                case "E":
                    Environment.Exit(0);
                    break;
                default:
                    DisplayConsoleMsg("Invalid selection. Exiting the application.", ConsoleMessageType.Error);
                    break;
            }
        }

        public static string ShowMenu(string version)
        {
            int selectedIndex = 0;
            ConsoleKey key;
            var options = new Dictionary<string, string>
            {
                { MigrationKey.ToString(), $"DB Migration ({MigrationKey})" },
                { UpdateKey.ToString(), $"DB Update ({UpdateKey})" },
                { ExitKey.ToString(), $"Exit ({ExitKey})" },
            };

            do
            {
                Console.Clear();
                var welcomeMessage = _config?.AppSettings.Settings["WelcomeMessage"]?.Value ?? "Welcome to the DbTool v.{0}";
                welcomeMessage = string.Format(welcomeMessage, version);
                DisplayConsoleMsg(welcomeMessage, ConsoleMessageType.Info);
                string strMsg = $"Use Up/Down arrows to navigate and Enter to select an option or enter{Environment.NewLine}"
                    + $"       '{MigrationKey}' for DB Migration or{Environment.NewLine}"
                    + $"       '{UpdateKey}' for DB Update or{Environment.NewLine}"
                    + $"       '{ExitKey}' for Exit :";
                DisplayConsoleMsg(strMsg, ConsoleMessageType.Info);

                for (int i = 0; i < options.Count; i++)
                {
                    if (i == selectedIndex)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"> {options.ElementAt(i).Value}");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.WriteLine($"  {options.ElementAt(i).Value}");
                    }
                }

                key = Console.ReadKey(true).Key;

                if (key == ConsoleKey.UpArrow)
                {
                    selectedIndex = (selectedIndex == 0) ? options.Count - 1 : selectedIndex - 1;
                }
                else if (key == ConsoleKey.DownArrow)
                {
                    selectedIndex = (selectedIndex == options.Count - 1) ? 0 : selectedIndex + 1;
                }
            }
            while (key != ConsoleKey.Enter && key != MigrationKey && key != UpdateKey && key != ExitKey);

            if (key == MigrationKey || key == UpdateKey || key == ExitKey)
            {
                return key.ToString();
            }

            return options.ElementAt(selectedIndex).Key;
        }

        private static Configuration GetConfigFile(string environmentName)
        {
            var configFile = $"App.{environmentName}.config";

            var map = new ExeConfigurationFileMap { ExeConfigFilename = configFile };
            var config = ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.None);

            return config;
        }
    }
}
