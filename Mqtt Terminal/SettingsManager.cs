using System;
using System.IO;
using System.Windows;
using Newtonsoft.Json;

namespace Mqtt_Terminal
{
	public class SettingsManager
	{
		private readonly string _pathToSettings = Environment.ExpandEnvironmentVariables(Properties.Settings.Default.DataPath);

		public SettingsManager()
		{
			// Try to load settings if they already exist
			if (File.Exists(_pathToSettings))
				try
				{
					CurrentSettings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(_pathToSettings));

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
			File.WriteAllText(_pathToSettings, JsonConvert.SerializeObject(CurrentSettings, Formatting.Indented));
		}
	}
}