using System;
using System.Linq;
using System.Windows;

namespace Mqtt_Terminal
{
    /// <summary>
    /// Interaction logic for ConnectionWindow.xaml
    /// </summary>
    public partial class ConnectionWindow : Window
    {
        private MessageBrokerClient _broker;
        private readonly Connection _connection;

        public ConnectionWindow(Connection connection)
        {
            InitializeComponent();
            DataContext = connection;

            _connection = connection;
            _subscriptions = _connection.SerializedSubscriptions.Select(ss => new Subscription(ss.Topic, arg => ReceivedSubscription(ss, arg), ss.Qos)).ToArray();

            Title = $"Connection {_connection.Name}";
            ConnectButton.Content = $"Try connect to {_connection.Hostname}";

            QosComboBox.SelectedItem = Qos.ExactlyOnce;
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
            {
                await _broker.ConnectAsync(_connection.SubscribeWhenOpened, _subscriptions);
            }
            else
            {
                await _broker.ConnectAsync(_connection.SubscribeWhenOpened);
            }

            CheckSubscriptionState();
        }

        private async void SubscribeButton_Click(object sender, RoutedEventArgs e)
        {
            await _broker.ConnectAsync(true, _subscriptions);
            CheckSubscriptionState();
        }

        private Subscription[] _subscriptions;

        private void ReceivedSubscription(SerializedSubscription ss, ReceivedMessageArguments arg)
        {
            ReceivedListView.Items.Add(arg);

            if(ReceivedListView.Items.Count > _connection.MaxMessagesStored)
            {
                ReceivedListView.Items.RemoveAt(0);
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (_broker != null) _broker.SuspendReconnecting = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _broker = new MessageBrokerClient(_connection.Hostname, _connection.ClientId, _connection.CleanSession, _connection.ConnectOnFailure, _connection.SubscribeOnFailure, _connection.ReconnectionInterval);
            }
            catch (Exception)
            {
                MessageBox.Show("Invalid hostname");
                Close();
                return;
            }
            _broker.ConnectionStateChanged += _broker_ConnectionStateChanged;

            if (_connection.ConnectWhenOpened)
            {
                ConnectButton_Click(null, null);
            }
        }

        private void PostTopic_Click(object sender, RoutedEventArgs e)
        {
            _broker.Publish(TopicBox.Text, ContentBox.Text, (Qos)QosComboBox.SelectedItem, Retain.IsChecked ?? false);
        }
    }
}
