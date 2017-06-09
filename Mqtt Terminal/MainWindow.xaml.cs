using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Mqtt_Terminal.MainView;
using Xceed.Wpf.AvalonDock.Layout;

namespace Mqtt_Terminal
{
	/// <summary>
	///     Interaktionslogik für MainWindow.xaml
	/// </summary>
	public partial class MainWindow
	{
		private readonly LayoutService _layoutService;

		public MainWindow()
		{
			InitializeComponent();

			SaveCommandBinding.Command = SaveCommand;
			SaveMenuItem.Command = SaveCommand;

			SaveLayoutCommandBinding.Command = SaveLayoutCommand;
			SaveLayoutMenuItem.Command = SaveLayoutCommand;

			ReloadLayoutCommandBinding.Command = ReloadLayoutCommand;
			ReloadLayoutMenuItem.Command = ReloadLayoutCommand;

			_layoutService = new LayoutService();

			CheckMenuItems();

			var anchorable = new LayoutAnchorable
			{
				Content = new ConnectionOverview(DisplayConnection),
				Title = Properties.Resources.Connections,
				ContentId = "ConnectionsId",
				IsActive = true,
				IsSelected = true,
				CanFloat = true,
				CanAutoHide = true,
				CanClose = false,
				CanHide = false
			};

			anchorable.AddToLayout(DockingManager, AnchorableShowStrategy.Top);


			OpenConnectionWithAutoOpen();
		}

		private RoutedUICommand ReloadLayoutCommand { get; } = new RoutedUICommand(Properties.Resources.LayoutReload,
			Properties.Resources.LayoutReload, typeof(MainWindow),
			new InputGestureCollection
			{
				new KeyGesture(Key.R, ModifierKeys.Alt)
			});

		private RoutedUICommand SaveLayoutCommand { get; } = new RoutedUICommand(Properties.Resources.LayoutSave,
			Properties.Resources.LayoutSave, typeof(MainWindow),
			new InputGestureCollection
			{
				new KeyGesture(Key.S, ModifierKeys.Alt)
			});

		private RoutedUICommand SaveCommand { get; } = new RoutedUICommand(Properties.Resources.SaveConfiguration,
			Properties.Resources.SaveConfiguration, typeof(MainWindow),
			new InputGestureCollection
			{
				new KeyGesture(Key.S, ModifierKeys.Control)
			});

		private string CurrentCulture
		{
			get => SettingsManager.Instance.CurrentSettings.Language;
			set
			{
				if (SettingsManager.Instance.CurrentSettings.Language == value)
				{
					CheckMenuItems();
					return;
				}
				SettingsManager.Instance.CurrentSettings.Language = value;
				CheckMenuItems();
				MessageBox.Show("Please save the settings and restart for the language to change", "Restart required");
			}
		}

		private void OpenConnectionWithAutoOpen()
		{
			foreach (var connection in SettingsManager.Instance.CurrentSettings.Connections.Where(c => c.OpenOnStartup))
				DisplayConnection(connection);
		}

		private void DisplayConnection(Connection connection)
		{
			var anchorable = new LayoutAnchorable
			{
				Content = new ConnectionControl(connection),
				Title = $"Connection {connection.Name}",
				ContentId = $"Connection{connection.Name}Id",
				IsActive = true,
				IsSelected = true,
				CanFloat = true,
				CanAutoHide = true,
				CanClose = true,
				CanHide = false
			};

			anchorable.AddToLayout(DockingManager, AnchorableShowStrategy.Top);
		}

		private void CheckMenuItems()
		{
			GermanMenu.IsChecked = false;
			EnglishMenu.IsChecked = false;

			switch (CurrentCulture)
			{
				case "de":
					GermanMenu.IsChecked = true;
					break;
				// case "en":
				default:
					EnglishMenu.IsChecked = true;
					break;
			}
		}

		private void Select_English(object sender, RoutedEventArgs e)
		{
			CurrentCulture = "en";
		}

		private void Select_German(object sender, RoutedEventArgs e)
		{
			CurrentCulture = "de";
		}

		private void Window_Closed(object sender, EventArgs e)
		{
			Application.Current.Shutdown();
		}

		private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
		{
			_layoutService.Initialize(DockingManager);
		}

//		private void MainWindow_OnClosing(object sender, CancelEventArgs e)
//		{
//			const MessageBoxButton buttons = MessageBoxButton.YesNoCancel;
//			const string message = "Save changes?";
//			const string caption = "Save?";
//			var result = MessageBox.Show(message, caption, buttons, MessageBoxImage.Question);
//
//			switch (result)
//			{
//				case MessageBoxResult.Yes:
//					SettingsManager.Instance.SaveSettings();
//					break;
//				case MessageBoxResult.Cancel:
//					e.Cancel = true;
//					break;
//				case MessageBoxResult.No:
//					break;
//				default:
//					throw new ArgumentOutOfRangeException();
//			}
//		}

		private void ReloadLayout_OnExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			_layoutService.LoadLayout();
		}

		private void SaveLayout_OnExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			_layoutService.SaveLayout();
		}

		private void SaveConfiguration_OnExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			SettingsManager.Instance.SaveSettings();
		}
	}
}