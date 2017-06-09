using System;
using System.IO;
using System.Windows;
using Xceed.Wpf.AvalonDock;
using Xceed.Wpf.AvalonDock.Layout.Serialization;

namespace Mqtt_Terminal.MainView
{
	public class LayoutService
	{
		private DockingManager _dockingManager;

		private static readonly string LayoutPath = Environment.ExpandEnvironmentVariables(Properties.Settings.Default.LayoutPath);

		/// <summary>
		///     Initializes this Service fully
		/// </summary>
		/// <param name="dockingManager">Used to save the layout</param>
		public void Initialize(DockingManager dockingManager)
		{
			_dockingManager = dockingManager;

			LoadLayout();
		}

		public void LoadLayout()
		{
			var folder = Path.GetDirectoryName(LayoutPath);
			if (folder != null && !Directory.Exists(folder))
			{
				Directory.CreateDirectory(folder);
			}

			if (File.Exists(LayoutPath))
			{
				try
				{
					using (var stream = File.Open(LayoutPath, FileMode.OpenOrCreate))
					{
						new XmlLayoutSerializer(_dockingManager).Deserialize(stream);
					}
				}
				catch (Exception)
				{
					MessageBox.Show("Error loading layout!");
				}
			}
			else
			{
				SaveLayout();
			}
		}

		public void SaveLayout()
		{
			new XmlLayoutSerializer(_dockingManager).Serialize(LayoutPath);
		}
	}
}
