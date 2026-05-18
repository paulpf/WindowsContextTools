using System.Diagnostics;
using System.Security.Principal;
using System.Windows;
using WindowsExplorerContextTools.Commands;
using WindowsExplorerContextTools.Services;

namespace WindowsExplorerContextTools
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow
	{
		private readonly List<string> m_SelectedPaths;
		private readonly string m_CurrentPath;
		private readonly List<IToolCommand> m_Commands;
		private readonly IResultOutputService m_ResultOutputService;
		private CancellationTokenSource m_CancellationTokenSource;
		private PauseTokenSource m_PauseTokenSource;
		private CommandContext? m_ActiveContext;
		private ActionState m_State = ActionState.Idle;

		private enum ActionState { Idle, Running, Paused }

		public MainWindow(List<string> selectedPaths, IEnumerable<IToolCommand> commands, IResultOutputService resultOutputService)
		{
			m_SelectedPaths = selectedPaths;
			m_ResultOutputService = resultOutputService;
			m_CurrentPath = selectedPaths.Count == 1 ? selectedPaths[0] : string.Empty;
			m_CancellationTokenSource = new CancellationTokenSource();
			m_PauseTokenSource = new PauseTokenSource();
			m_Commands = commands.ToList();

			InitializeComponent();
			InitializeCommandList();
		}

		private void InitializeCommandList()
			{
				foreach (var command in m_Commands)
				{
					comboBoxCommands.Items.Add(command.Name);
				}

				// Click-Handler für Output-Datei-Link
				txtOutputFile.MouseDown += (s, e) =>
				{
					if (!string.IsNullOrEmpty(txtOutputFile.Text) && txtOutputFile.Text.Contains(":"))
					{
						var filePath = txtOutputFile.Text.Replace("📄 Live results: ", "").Trim();
						if (System.IO.File.Exists(filePath))
						{
							m_ResultOutputService.ShowFileInExplorer(filePath);
						}
					}
				};
			}

		private async void ButtonAction_Click(object sender, RoutedEventArgs e)
		{
			switch (m_State)
			{
				case ActionState.Idle:
					await ExecuteCommandAsync();
					break;
				case ActionState.Running:
					await PauseAndPromptAsync();
					break;
				case ActionState.Paused:
					ResumeExecution();
					break;
			}
		}

		private static bool IsRunningAsAdministrator()
		{
			using var identity = WindowsIdentity.GetCurrent();
			var principal = new WindowsPrincipal(identity);
			return principal.IsInRole(WindowsBuiltInRole.Administrator);
		}

		private bool OfferAdminRestartIfNeeded()
		{
			if (IsRunningAsAdministrator())
			{
				return false;
			}

			var result = MessageBox.Show(
				this,
				"Fuer vollen Zugriff auf alle Verzeichnisse sind Admin-Rechte erforderlich.\n\nAls Administrator neu starten?",
				"Eingeschraenkter Zugriff",
				MessageBoxButton.YesNo,
				MessageBoxImage.Question);

			if (result != MessageBoxResult.Yes)
			{
				return false;
			}

			try
			{
				var startInfo = new ProcessStartInfo
				{
					FileName = Environment.ProcessPath!,
					Arguments = string.Join(" ", Environment.GetCommandLineArgs().Skip(1).Select(a => $"\"{a}\"")),
					Verb = "runas",
					UseShellExecute = true
				};
				Process.Start(startInfo);
				Application.Current.Shutdown();
				return true;
			}
			catch (System.ComponentModel.Win32Exception)
			{
				return false;
			}
		}

		private async Task ExecuteCommandAsync()
		{
			try
			{
				var command = GetSelectedCommand();
				if (command == null)
				{
					return;
				}

				if (OfferAdminRestartIfNeeded())
				{
					return;
				}

				m_CancellationTokenSource = new CancellationTokenSource();
				m_PauseTokenSource = new PauseTokenSource();
				SetState(ActionState.Running);

				try
				{
					m_ActiveContext = CreateCommandContext();
					var result = await command.ExecuteAsync(m_ActiveContext, m_CancellationTokenSource.Token);
					HandleCommandResult(result);
				}
				catch (OperationCanceledException)
				{
					// cancelled via dialog or window close
				}
				finally
				{
					m_ActiveContext = null;
					SetState(ActionState.Idle);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, $"An unexpected error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private async Task PauseAndPromptAsync()
		{
			m_PauseTokenSource.Pause();
			SetState(ActionState.Paused);
			btnAction.IsEnabled = false;

			var partialResults = m_ActiveContext?.CollectedResults;
			var count = partialResults?.Count ?? 0;

			var answer = MessageBox.Show(
				this,
				count > 0
					? $"{count} items found so far.\n\nShow partial results in editor?"
					: "No items found so far.\n\nShow partial results in editor?",
				"Paused",
				MessageBoxButton.YesNo,
				MessageBoxImage.Question);

			if (answer == MessageBoxResult.Yes && count > 0)
			{
				await m_ResultOutputService.ShowInEditorAsync(partialResults!.ToList(), CancellationToken.None);
				btnAction.IsEnabled = true;
			}
			else
			{
				m_PauseTokenSource.Resume();
				m_CancellationTokenSource.Cancel();
				Close();
			}
		}

		private void ResumeExecution()
		{
			m_PauseTokenSource.Resume();
			SetState(ActionState.Running);
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			m_PauseTokenSource.Resume();
			m_CancellationTokenSource.Cancel();
			m_CancellationTokenSource.Dispose();
		}

		private IToolCommand? GetSelectedCommand()
		{
			string selectedCommandName = comboBoxCommands.SelectedItem?.ToString() ?? string.Empty;

			if (string.IsNullOrEmpty(selectedCommandName))
			{
				MessageBox.Show("Please select a command.");
				return null;
			}

			var command = m_Commands.FirstOrDefault(c => c.Name == selectedCommandName);

			if (command == null)
			{
				MessageBox.Show("Invalid command.");
			}

			return command;
		}

		private CommandContext CreateCommandContext()
			{
				var progress = new Progress<Commands.ProgressInfo>(info =>
				{
					// Zeige Duplikate wenn vorhanden, sonst normale Verarbeitung
					if (info.DuplicateCount.HasValue)
					{
						txtProgress.Text = info.DuplicateCount == 0
							? "0 duplicates found"
							: $"{info.DuplicateCount} duplicate{(info.DuplicateCount == 1 ? "" : "s")} found";
					}
					else
					{
						txtProgress.Text = info.TotalCount.HasValue
							? $"{info.ProcessedCount} / {info.TotalCount}"
							: $"{info.ProcessedCount} processed";
					}

					// Zeige Output-Datei-Link wenn vorhanden
					if (!string.IsNullOrEmpty(info.OutputFilePath))
					{
						txtOutputFile.Text = $"📄 Live results: {info.OutputFilePath}";
						txtOutputFile.TextDecorations = System.Windows.TextDecorations.Underline;
						txtOutputFile.Cursor = System.Windows.Input.Cursors.Hand;
					}
				});

				return new CommandContext
				{
					SelectedPaths = m_SelectedPaths,
					CurrentPath = m_CurrentPath,
					InputText = textBoxCommandInput.Text,
					Progress = progress,
				PauseToken = m_PauseTokenSource.Token
			};
		}

		private void HandleCommandResult(CommandResult result)
		{
			if (result.ErrorMessage != null)
			{
				MessageBox.Show(result.ErrorMessage);
			}

			if (result.ShouldClose)
			{
				Close();
			}
		}

		private void SetState(ActionState state)
		{
			m_State = state;

			switch (state)
			{
				case ActionState.Idle:
					btnAction.Content = "Run";
					progressBar.IsIndeterminate = false;
					progressBar.Visibility = Visibility.Collapsed;
					txtProgress.Text = string.Empty;
					txtProgress.Foreground = System.Windows.Media.Brushes.Gray;
					txtOutputFile.Text = string.Empty;
					break;
				case ActionState.Running:
					btnAction.Content = "Cancel";
					progressBar.IsIndeterminate = true;
					progressBar.Visibility = Visibility.Visible;
					txtProgress.Foreground = System.Windows.Media.Brushes.Gray;
					txtOutputFile.Text = string.Empty;
					break;
				case ActionState.Paused:
					btnAction.Content = "Resume";
					progressBar.IsIndeterminate = false;
					txtProgress.Foreground = System.Windows.Media.Brushes.Orange;
					break;
			}
		}
	}
}