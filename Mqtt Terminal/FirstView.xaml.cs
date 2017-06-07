using System.Windows.Controls;

namespace Mqtt_Terminal
{
    /// <summary>
    /// Interaction logic for FirstView.xaml
    /// </summary>
    public partial class FirstView : UserControl
    {
        private MessageBrokerClient _broker;

        public FirstView()
        {
            InitializeComponent();

            _broker = new MessageBrokerClient("localhost", Qos.Default, false);
            _broker.ConnectionStateChanged += _broker_ConnectionStateChanged;
        }

        private void _broker_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            IsConnectedText.Text = $"Is connected: {e.IsConnected}";
        }

        private async void ConnectButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            await _broker.ConnectAsync();
        }
    }
}
