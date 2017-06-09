using System;
using System.Windows;
using System.Windows.Controls;

namespace Mqtt_Terminal
{
	/// <summary>
	/// Interaction logic for ConnectionOverview.xaml
	/// </summary>
	public partial class ConnectionOverview
	{
		private readonly Action<Connection> _displayConnection;

		public ConnectionOverview(Action<Connection> displayConnection)
		{
			_displayConnection = displayConnection;
			InitializeComponent();

			DisplayConnectionInListView();
		}

		private void Add_Connection(object sender, RoutedEventArgs e)
		{
			// Create default connection
			var connection = new Connection();

			// Let user edit it
			var save = new EditConnectionWindow(connection).ShowDialog();

			if (save == true)
				SettingsManager.Instance.CurrentSettings.Connections.Add(connection);

			// Update view
			DisplayConnectionInListView();
		}

		private void Edit_Connection(object sender, RoutedEventArgs e)
		{
			// Clicked connection
			var connection = (Connection)((Button)sender).DataContext;

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
			var connection = (Connection)((Button)sender).DataContext;

			// Remove it
			SettingsManager.Instance.CurrentSettings.Connections.Remove(connection);

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
			var connection = (Connection)((Button)sender).DataContext;

			// Open it
			_displayConnection(connection);
		}
	}
}
