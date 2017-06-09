using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Mqtt_Terminal
{
	/// <summary>
	///     Interaction logic for ConnectionWindow.xaml
	/// </summary>
	public partial class ConnectionControl
	{
		private readonly List<ReceivedMessageArguments> _allMessages = new List<ReceivedMessageArguments>();
		private readonly Connection _connection;

		private readonly Subscription[] _subscriptions;
		private readonly MessageBrokerClient _broker;

		private bool _subscribedToClose;

		public ConnectionControl(Connection connection)
		{
			InitializeComponent();
			DataContext = connection;

			_connection = connection;
			_subscriptions = _connection.SerializedSubscriptions
				.Select(ss => new Subscription(ss.Topic, arg => ReceivedSubscription(ss, arg), ss.Qos)).ToArray();

			ConnectButton.Content = $"Try connect to {_connection.Hostname}";

			QosComboBox.SelectedItem = Qos.ExactlyOnce;

			try
			{
				_broker = new MessageBrokerClient(_connection.Hostname, _connection.ClientId, _connection.CleanSession,
					_connection.ConnectOnFailure, _connection.SubscribeOnFailure, _connection.ReconnectionInterval);
			}
			catch (Exception)
			{
				MessageBox.Show("Invalid hostname");
				//TODO Close();
				return;
			}
			_broker.ConnectionStateChanged += _broker_ConnectionStateChanged;

			if (_connection.ConnectWhenOpened)
				ConnectButton_Click(null, null);
		}

		private void _broker_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
		{
			IsConnectedText.Text = $"Is connected: {e.IsConnected}";
			SubscribeButton.IsEnabled = e.IsConnected;
			CheckSubscriptionState();
		}

		private void CheckSubscriptionState()
		{
			IsSubscribedText.Text = $"Is subscribed: {_broker.IsConnected && _broker.HasSubscriptions}";
		}

		private async void ConnectButton_Click(object sender, RoutedEventArgs e)
		{
			if (_connection.SubscribeWhenOpened)
				await _broker.ConnectAsync(_connection.SubscribeWhenOpened, _subscriptions);
			else
				await _broker.ConnectAsync(_connection.SubscribeWhenOpened);

			CheckSubscriptionState();
		}

		private async void SubscribeButton_Click(object sender, RoutedEventArgs e)
		{
			await _broker.ConnectAsync(true, _subscriptions);
			CheckSubscriptionState();
		}

		private void ReceivedSubscription(SerializedSubscription ss, ReceivedMessageArguments arg)
		{
			_allMessages.Add(arg);

			if (_allMessages.Count > _connection.MaxMessagesStored)
				_allMessages.RemoveAt(0);

			ApplyFilter();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			var window = Window.GetWindow(this);
			if (window != null && !_subscribedToClose)
			{
				window.Closed += async (o, args) =>
				{
					if (_broker != null) await _broker.DisconnectAsync();
				};

				_subscribedToClose = true;
			}
		}

		private void PostTopic_Click(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(TopicBox.Text))
			{
				MessageBox.Show("Topic is mandatory");
				return;
			}

			_broker.Publish(TopicBox.Text, ContentBox.Text, (Qos) QosComboBox.SelectedItem, Retain.IsChecked ?? false);
		}

		private void FilterText_TextChanged(object sender, TextChangedEventArgs e)
		{
			ApplyFilter();
		}

		private void ApplyFilter()
		{
			ReceivedListView.Items.Clear();

			if (string.IsNullOrEmpty(FilterText.Text))
				foreach (var item in _allMessages)
					ReceivedListView.Items.Add(item);
			else
				foreach (var item in _allMessages.Where(m => m.Content.ToLowerInvariant()
					.Contains(FilterText.Text.ToLowerInvariant())))
					ReceivedListView.Items.Add(item);
		}
	}
}