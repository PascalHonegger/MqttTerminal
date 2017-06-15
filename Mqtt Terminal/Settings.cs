using System;
using System.Collections.Generic;
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Mqtt_Terminal
{
	[Serializable]
	public class Settings
	{
		public string Language { get; set; } = "en";

		public List<Connection> Connections { get; set; } = new List<Connection>();
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

		public bool CleanSession { get; set; }

		public bool ConnectOnFailure { get; set; } = true;

		public bool SubscribeOnFailure { get; set; } = true;

		public int ReconnectionInterval { get; set; } = 5;

		public List<SerializedSubscription> SerializedSubscriptions { get; set; } = new List<SerializedSubscription>();

		public bool OpenOnStartup { get; set; } = true;

		public bool ConnectWhenOpened { get; set; } = true;

		public bool SubscribeWhenOpened { get; set; } = true;

		public int MaxMessagesStored { get; set; } = 5000;
	}

	[Serializable]
	public class SerializedSubscription
	{
		public string Topic { get; set; } = "#";
		public Qos Qos { get; set; }
	}
}