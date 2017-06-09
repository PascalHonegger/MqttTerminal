using System;
using System.IO;
using System.Windows;
using System.Xml.Serialization;

namespace Mqtt_Terminal
{
	public class SettingsManager
	{
		private readonly XmlSerializer _serializer = new XmlSerializer(typeof(Settings), "Mqtt.Terminal");

		private readonly string _pathToSettings = Path.Combine(Path.GetTempPath(), "MqttSettings.xml");

		public SettingsManager()
		{
			// Try to load settings if they already exist
			if (File.Exists(_pathToSettings))
				try
				{
					var contentIfExists = File.ReadAllText(_pathToSettings);

					var reader = new StringReader(contentIfExists);

					CurrentSettings = (Settings) _serializer.Deserialize(reader);
					return;
				}
				catch (Exception e)
				{
					MessageBox.Show(e.Message, "Error loading settings", MessageBoxButton.OK, MessageBoxImage.Error);
				}

			// Use default settings if file couldn't be loaded
			CurrentSettings = new Settings();

			// Save these default settings for the future
			SaveSettings();
		}

		public static SettingsManager Instance { get; } = new SettingsManager();

		public Settings CurrentSettings { get; }

		public void SaveSettings()
		{
			File.WriteAllText(_pathToSettings, "");

			var stream = File.OpenWrite(_pathToSettings);

			_serializer.Serialize(stream, CurrentSettings);
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
			var now = DateTime.Now;
			var timeRange = now - now.Date;
			ClientId = $"{Environment.MachineName}-{(int) timeRange.TotalMilliseconds}";
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