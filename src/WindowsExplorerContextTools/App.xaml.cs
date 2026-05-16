using System.IO;
using System.Windows;
using WindowsExplorerContextTools.Commands;
using WindowsExplorerContextTools.Services;

namespace WindowsExplorerContextTools
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var filePaths = CollectValidPaths(e.Args);
            var resultOutputService = new ResultOutputService();
            var commands = CreateCommands(resultOutputService);

            MainWindow mainWindow = new MainWindow(filePaths, commands, resultOutputService);
            mainWindow.Show();
        }

        private static List<string> CollectValidPaths(string[] args)
        {
            List<string> filePaths = new List<string>();

            foreach (var arg in args)
            {
                if (string.IsNullOrWhiteSpace(arg))
                {
                    continue;
                }

                // Windows shell can pass paths like Y:\" where the backslash escapes the closing quote,
                // leaving a trailing " character in the argument.
                var cleanedPath = arg.TrimEnd('"');

                if (!string.IsNullOrWhiteSpace(cleanedPath))
                {
                    filePaths.Add(cleanedPath);
                }
            }

            if (filePaths.Count == 0 && args.Length > 0)
            {
                MessageBox.Show(
                    $"No valid paths could be resolved.\n\nReceived arguments:\n{string.Join("\n", args)}",
                    "MyContextTools - Debug",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            return filePaths;
        }

        private static List<IToolCommand> CreateCommands(IResultOutputService resultOutputService)
        {
            var fileSystemService = new FileSystemService();
            var duplicateFileService = new DuplicateFileService(fileSystemService);

            return
            [
                new ListFilesAndFoldersCommand(fileSystemService, resultOutputService),
                new ListFilesCommand(fileSystemService, resultOutputService),
                new ListFoldersCommand(fileSystemService, resultOutputService),
                new ListFoldersCommand(fileSystemService, resultOutputService, includeSubfolders: true),
                new FindDuplicateFilesCommand(fileSystemService, duplicateFileService, resultOutputService),
                new FindSmallestSolutionCommand(fileSystemService, resultOutputService)
            ];
        }
    }
}
