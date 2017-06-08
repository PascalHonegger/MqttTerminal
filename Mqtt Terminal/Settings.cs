using System;
using System.IO;
using System.Windows;
using System.Xml.Serialization;

namespace Mqtt_Terminal
{
    public class SettingsManager
    {
        public static SettingsManager Instance { get; } = new SettingsManager();

        private string PathToSettings = Path.Combine(Path.GetTempPath(), "MqttSettings.xml");

        private XmlSerializer _serializer = new XmlSerializer(typeof(Settings), "Mqtt.Terminal");

        public Settings CurrentSettings { get; }

        public void SaveSettings()
        {
            File.WriteAllText(PathToSettings, "");

            var stream = File.OpenWrite(PathToSettings);

            _serializer.Serialize(stream, CurrentSettings);
        }

        public SettingsManager()
        {
            // Try to load settings if they already exist
            if (File.Exists(PathToSettings))
            {
                try
                {
                    var contentIfExists =  File.ReadAllText(PathToSettings);

                    var reader = new StringReader(contentIfExists);

                    CurrentSettings = (Settings)_serializer.Deserialize(reader);
                    return;
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "Error loading settings", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            // Use default settings if file couldn't be loaded
            CurrentSettings = new Settings();

            // Save these default settings for the future
            SaveSettings();
        }
    }

    [Serializable]
    public class Settings
    {
        public string Language { get; set; } = "en";

        public Connection[] Connections { get; set; } = new Connection[0];
    }

    [Serializable]
    public class Connection
    {
        public Connection()
        {
            ClientId = $"{Environment.MachineName}-{DateTime.Now.Ticks}";
            Name = $"{ClientId}@{Hostname}";
        }

        public string Name { get; set; }

        public string Hostname { get; set; } = "127.0.0.1";

        public string ClientId { get; set; }

        public bool CleanSession { get; set; } = true;

        public bool ConnectOnFailure { get; set; } = true;

        public bool SubscribeOnFailure { get; set; } = true;

        public int ReconnectionInterval { get; set; } = 5;

        public SerializedSubscription[] SerializedSubscriptions { get; set; } = new SerializedSubscription[0];

        public bool OpenOnStartup { get; set; } = false;

        public bool ConnectWhenOpened { get; set; } = true;

        public bool SubscribeWhenOpened { get; set; } = false;

        public int MaxMessagesStored { get; set; } = 5000;
    }

    [Serializable]
    public class SerializedSubscription
    {
        public string Topic { get; set; }
        public Qos Qos { get; set; }
    }
}
