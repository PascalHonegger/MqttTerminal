using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Mqtt_Terminal
{
	/// <summary>
	///     Interaktionslogik für MainWindow.xaml
	/// </summary>
	public partial class MainWindow
	{
		public MainWindow()
		{
			InitializeComponent();

			CheckMenuItems();

			OpenConnectionWithAutoOpen();

			DisplayConnectionInListView();
		}

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
				new ConnectionWindow(connection).Show();
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

		private void Save_Configuration(object sender, RoutedEventArgs e)
		{
			SettingsManager.Instance.SaveSettings();
		}

		private void Select_English(object sender, RoutedEventArgs e)
		{
			CurrentCulture = "en";
		}

		private void Select_German(object sender, RoutedEventArgs e)
		{
			CurrentCulture = "de";
		}

		private void Add_Connection(object sender, RoutedEventArgs e)
		{
			// Create default connection
			var connection = new Connection();

			// Let user edit it
			var save = new EditConnectionWindow(connection).ShowDialog();

			if (save == true)
				SettingsManager.Instance.CurrentSettings.Connections = SettingsManager.Instance.CurrentSettings.Connections
					.Concat(new[] {connection}).ToArray();

			// Update view
			DisplayConnectionInListView();
		}

		private void Edit_Connection(object sender, RoutedEventArgs e)
		{
			// Clicked connection
			var connection = (Connection) ((Button) sender).DataContext;

			// Let user edit it
			var save = new EditConnectionWindow(connection).ShowDialog();

			if (save != true)
			{
				// TODO edit Cancel => Revert changes
			}

			// Update view
			DisplayConnectionInListView();
		}

		private void Remove_Connection(object sender, RoutedEventArgs e)
		{
			// Clicked connection
			var connection = (Connection) ((Button) sender).DataContext;

			// Remove it
			SettingsManager.Instance.CurrentSettings.Connections = SettingsManager.Instance.CurrentSettings.Connections
				.Except(new[] {connection}).ToArray();

			// Update view
			DisplayConnectionInListView();
		}

		private void DisplayConnectionInListView()
		{
			ConListView.Items.Clear();

			foreach (var connection in SettingsManager.Instance.CurrentSettings.Connections)
				ConListView.Items.Add(connection);
		}

		private void Open_Connection(object sender, RoutedEventArgs e)
		{
			// Clicked connection
			var connection = (Connection) ((Button) sender).DataContext;

			// Open it
			new ConnectionWindow(connection).Show();
		}

		private void Window_Closed(object sender, EventArgs e)
		{
			Application.Current.Shutdown();
		}
	}
}