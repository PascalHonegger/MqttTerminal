using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Mqtt_Terminal
{
    /// <summary>
    /// Interaction logic for EditConnectionWindow.xaml
    /// </summary>
    public partial class EditConnectionWindow : Window
    {
        private readonly Connection _toEdit;

        public static List<Qos> AllQos { get; } = Enum.GetValues(typeof(Qos)).Cast<Qos>().ToList();

        public EditConnectionWindow(Connection toEdit)
        {
            InitializeComponent();

            _toEdit = toEdit;
            DataContext = toEdit;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Add_Subscription(object sender, RoutedEventArgs e)
        {
            _toEdit.SerializedSubscriptions = _toEdit.SerializedSubscriptions.Concat(new[] { new SerializedSubscription() }).ToArray();
            DisplaySubscriptions();
        }

        private void DisplaySubscriptions()
        {
            SubListView.Items.Clear();

            foreach (var subscription in _toEdit.SerializedSubscriptions)
            {
                SubListView.Items.Add(subscription);
            }
        }

        private void Remove_Subscription(object sender, RoutedEventArgs e)
        {
            // Clicked connection
            var subscription = (SerializedSubscription)((Button)(sender)).DataContext;

            // Remove it
            _toEdit.SerializedSubscriptions = _toEdit.SerializedSubscriptions.Except(new[] { subscription }).ToArray();

            // Update view
            DisplaySubscriptions();
        }
    }
}
