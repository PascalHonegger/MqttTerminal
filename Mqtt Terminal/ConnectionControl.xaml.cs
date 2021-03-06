﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Mqtt_Terminal
{
	/// <summary>
	///     Interaction logic for ConnectionWindow.xaml
	/// </summary>
	public partial class ConnectionControl
	{
		private readonly List<ReceivedMessageArguments> _allMessages = new List<ReceivedMessageArguments>();
		private readonly ObservableCollection<ReceivedMessageArguments> _filteredMessages = new ObservableCollection<ReceivedMessageArguments>();

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

			CheckConnectedSubscribedGui();

			if (_connection.ConnectWhenOpened)
				ConnectButton_Click(null, null);
		}

		private void _broker_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
		{
			CheckConnectedSubscribedGui();
		}

		private void CheckConnectedSubscribedGui()
		{
			if (_broker == null) return;

			IsConnectedText.Text = $"Is connected: {_broker.IsConnected}";
			IsSubscribedText.Text = $"Is subscribed: {_broker.IsConnected && _broker.HasSubscriptions}";

			if (_broker.IsConnected)
			{
				if (!_broker.HasSubscriptions)
				{
					ConnectButton.Visibility = Visibility.Visible;
					ConnectButton.Content = "Subscribe to topics";
				}
				else
				{
					ConnectButton.Visibility = Visibility.Collapsed;
				}
			}
			else
			{
				ConnectButton.Visibility = Visibility.Visible;
				ConnectButton.Content = $"Connect to {_connection.Hostname}";
			}
		}

		private async void ConnectButton_Click(object sender, RoutedEventArgs e)
		{
			if (_broker.IsConnected || _connection.SubscribeWhenOpened)
				await _broker.ConnectAsync(true, _subscriptions);
			else
				await _broker.ConnectAsync(_connection.SubscribeWhenOpened);

			CheckConnectedSubscribedGui();
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
			var selected = ReceivedListView.SelectedItem;

			_filteredMessages.Clear();

			if (string.IsNullOrEmpty(FilterText.Text))
				foreach (var item in _allMessages)
					_filteredMessages.Add(item);
			else
			{
				var formattedFilterText = FilterText.Text.ToLowerInvariant();
				foreach (var item in _allMessages.Where(m =>
					m.Content.ToLowerInvariant().Contains(formattedFilterText) ||
					m.Topic.ToLowerInvariant().Contains(formattedFilterText)))
				{
					_filteredMessages.Add(item);
				}
			}

			ReceivedListView.SelectedItem = selected;

			if (_collectionBindingEstablished) return;
			ReceivedListView.ItemsSource = _filteredMessages;
			var view = (CollectionView)CollectionViewSource.GetDefaultView(ReceivedListView.ItemsSource);
			var groupDescription = new PropertyGroupDescription("Topic");
			view.GroupDescriptions?.Add(groupDescription);
			_collectionBindingEstablished = true;
		}

		private bool _collectionBindingEstablished = false;
	}
}