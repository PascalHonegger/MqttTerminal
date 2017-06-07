using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Exceptions;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace Mqtt_Terminal
{
    public class MessageBrokerClient
    {
        public MessageBrokerClient(string brokerAddress, Qos qos, bool cleanSession)
        {
            //TODO good?
            _clientId = HostName;
            _qos = qos;
            _cleanSession = cleanSession;

            _mqttClient = new MqttClient(brokerAddress);
            _mqttClient.ConnectionClosed += ReceivedMqttClientConnectionClosed;
            _mqttClient.MqttMsgPublishReceived += ReceivedMqttMessage;
            _mqttClient.MqttMsgPublished += ReceivedMqttMessagePublished;
        }

        public event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged = delegate { };

        public async Task ConnectAsync(string topicToSubscribe, Action<ReceivedMessageArguments> receivedMessageAction, Qos qos)
        {
            await ConnectAsync(new Subscription(topicToSubscribe, receivedMessageAction, qos));
        }

        public void Publish(string topic, string value, Qos qos = Qos.Default, bool retain = false, bool sendOnlyChanges = false)
        {
            try
            {
                if (sendOnlyChanges)
                {
                    bool isChangedValue = IsChangedValue(topic, value);
                    if (isChangedValue == false)
                    {
                        // Value has not changed
                        return;
                    }
                }

                 Qos qos2 = qos == Qos.Default ? _qos : qos;
                byte[] message = Encoding.UTF8.GetBytes(value);
                lock (_lock)
                {
                    ++_publishedButNotConfirmedMessages;

                    _resetEvent.Reset();
                }
                ushort id = _mqttClient.Publish(topic, message, (byte)qos2, retain);
            }
            catch (Exception ex)
            {
                //Ignored
            }
        }

        public bool IsChangedValue(string topic, string value)
        {
            var changed = true;
            object oldValue;
            if (_currentValueForTopic.TryGetValue(topic, out oldValue))
            {
                changed = !Equals(value, oldValue);
            }
            _currentValueForTopic[topic] = value;
            return changed;
        }

        public async Task DisconnectAsync()
        {
            _hasBeenDisconnected = true;

            if (_mqttClient.IsConnected)
            {
                try
                {
                    // Ensure, all messages are sent
                    int count;
                    lock (_lock)
                    {
                        count = _publishedButNotConfirmedMessages;
                    }

                    if (count > 0)
                    {
                        Task setEventTask = _resetEvent.WaitAsync();
                        Task timeOutTask = Task.Delay(TimeSpan.FromSeconds(1));
                        await Task.WhenAny(setEventTask, timeOutTask);
                    }

                    _mqttClient.MqttMsgPublishReceived -= ReceivedMqttMessage;
                    _mqttClient.ConnectionClosed -= ReceivedMqttClientConnectionClosed;
                    _mqttClient.MqttMsgPublished -= ReceivedMqttMessagePublished;

                    _mqttClient.Disconnect();
                }
                catch (Exception ex)
                {
                    //Ignored
                }
            }
        }

        public async Task ConnectAsync(params Subscription[] subscriptions)
        {
            //Only one reconnect at once
            using (await _reconnectionLock.LockAsync())
            {
                lock (_lock)
                {
                    _subscriptions = subscriptions;
                    _syncContext = SynchronizationContext.Current;
                }

                while (!_mqttClient.IsConnected && !_hasBeenDisconnected)
                {
                    try
                    {
                        if (!SuspendReconnecting)
                        {
                            if (SynchronizationContext.Current != null)
                            {
                                // We should not block this thread 
                                await Task.Run(() => Connect());
                            }
                            else
                            {
                                //Called by any background thread
                                Connect();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        //Ignore
                    }

                    if (!_mqttClient.IsConnected)
                    {
                        await Task.Delay(ReconnectIntervalInMs);
                    }
                }
                if (_mqttClient.IsConnected)
                {
                    ReceivedSuccessfullyReconnected();
                }
            }
        }

        private void TryToExecuteActionWithSynchronizationContext(Action actionToExcute)
        {
            if (actionToExcute == null)
            {
                throw new ArgumentNullException(nameof(actionToExcute));
            }

            SynchronizationContext syncContext;

            lock (_lock)
            {
                syncContext = _syncContext;
            }
            if (syncContext != null)
            {
                syncContext.Post(state => actionToExcute(), null);
            }
            else
            {
                actionToExcute();
            }
        }

        private void SubscribeInternal(Subscription subscription)
        {
            subscription.SentId = _mqttClient.Subscribe(new[] { subscription.Topic }, new[] { (byte)subscription.Qos });
        }

        /// <summary>
        /// object for simple Types
        /// JObject for complex Types
        /// </summary>
        /// <param name="args">received args from MQTT</param>
        /// <returns>ReceivedMessageArguments with content</returns>
        private ReceivedMessageArguments ParseContent(MqttMsgPublishEventArgs args)
        {
            return new ReceivedMessageArguments(args.Topic, Encoding.UTF8.GetString(args.Message))
            {
                IsRetainFlagSet = args.Retain,
                IsDuplicatedFlagSet = args.DupFlag
            };
        }

        private void ReceivedMqttMessagePublished(object sender, MqttMsgPublishedEventArgs e)
        {
            lock (_lock)
            {
                if (--_publishedButNotConfirmedMessages <= 0)
                {
                    _resetEvent.Set();
                }
            }
        }

        private void ReceivedMqttMessage(object sender, MqttMsgPublishEventArgs args)
        {
            Subscription subscription;

            lock (_lock)
            {
                subscription = _subscriptions.Length == 1 ? _subscriptions.First() : _subscriptions.FirstOrDefault(i => i.IsMatchingTopic(args.Topic));
            }

            Action actionToExecute = null;

            if (subscription != null)
            {
                actionToExecute = () => subscription.ReceivedMessageAction(ParseContent(args));
            }

            try
            {
                TryToExecuteActionWithSynchronizationContext(actionToExecute);
            }
            catch (Exception ex)
            {
                //Ignore
            }
        }

        private bool Connect()
        {
            try
            {
                _hasBeenDisconnected = false;
                byte code = _mqttClient.Connect(_clientId, null, null, _cleanSession, DefaultKeepAlivePeriod);
                if (code == 0)
                {
                    RaiseConnectionStateChanged(true);
                }
                else
                {
                    RaiseConnectionStateChanged(false);
                }
                return code == 0;
            }
            catch (MqttConnectionException ex)
            {
                RaiseConnectionStateChanged(false);
            }
            return false;
        }

        private async void Reconnect()
        {
            try
            {
                Subscription[] subscriptions;

                lock (_lock)
                {
                    subscriptions = _subscriptions;
                }

                await ConnectAsync(subscriptions);
            }
            catch (Exception ex)
            {
                //Ignore
            }
        }

        private void ReceivedSuccessfullyReconnected()
        {
            RaiseConnectionStateChanged(true);
            ResubscribeTopics();
        }

        private void ResubscribeTopics()
        {
            Subscription[] subscriptions;

            lock (_lock)
            {
                subscriptions = _subscriptions;
            }

            foreach (Subscription subscription in subscriptions)
            {
                SubscribeInternal(subscription);
            }
        }

        private void ReceivedMqttClientConnectionClosed(object sender, EventArgs e)
        {
            if (!IsConnected)
            {
                RaiseConnectionStateChanged(false);

                Reconnect();
            }
        }

        private void RaiseConnectionStateChanged(bool isConnected)
        {
            Application.Current.Dispatcher.Invoke(() => ConnectionStateChanged(this, new ConnectionStateChangedEventArgs(isConnected)));
        }

        public bool IsConnected => _mqttClient.IsConnected;

        public bool SuspendReconnecting { get; set; } = false;

        public int ReconnectIntervalInMs { get; } = 3000;

        public string HostName => Environment.MachineName;

        private int _publishedButNotConfirmedMessages;

        /// <summary>
        /// These are all the subscriptions a user of this clients wants to subscribe
        /// </summary>
        private Subscription[] _subscriptions;

        /// <summary>
        /// Locked, if any message are still in queue => Disconnect not before all messages are sent
        /// </summary>
        private readonly AsyncManualResetEvent _resetEvent = new AsyncManualResetEvent();

        private readonly object _lock = new object();
        private readonly MqttClient _mqttClient;
        private readonly AsyncLock _reconnectionLock = new AsyncLock();
        private bool _hasBeenDisconnected;
        private SynchronizationContext _syncContext;
        private readonly Dictionary<string, object> _currentValueForTopic = new Dictionary<string, object>();
        private const int DefaultKeepAlivePeriod = 30;
        private string _clientId;
        private Qos _qos;
        private bool _cleanSession;
    }

    /// <summary>
    /// MQTT Quality of service level
    /// see also http://www.hivemq.com/blog/mqtt-essentials-part-6-mqtt-quality-of-service-levels
    /// </summary>
    public enum Qos
    {
        /// <summary>
        /// Default from defined ITopic or from Constanst
        /// </summary>
        Default = -1,

        /// <summary>
        /// Qos 0: Fire and forget - the message may not be delivered
        /// </summary>
        AtMostOnce = 0,

        /// <summary>
        /// Qos 1: At least once - the message will be delivered, but may be delivered more than once in some circumstances.
        /// </summary>
        AtLeastOnce = 1,

        /// <summary>
        /// Once and one only - the message will be delivered exactly once.
        /// </summary>
        ExactlyOnce = 2,

        /// <summary>
        /// Returned from Library, if subscribe to broker was not successful
        /// </summary>
        GrantedFailure = 128
    }

    public class ConnectionStateChangedEventArgs : EventArgs
    {
        public ConnectionStateChangedEventArgs(bool isConnected)
        {
            IsConnected = isConnected;
        }

        public bool IsConnected { get; }
    }

    public class ReceivedMessageArguments
    {
        public ReceivedMessageArguments(string topic, string content)
        {
            Topic = topic;
            Content = content;
        }

        public ReceivedMessageArguments(string topic) : this(topic, null)
        {
            Topic = topic;
        }

        public object ConvertContentTo(Type type)
        {
            return Convert.ChangeType(Content, type);
        }

        public string Topic { get; }

        public string Content { get; set; }

        public bool IsRetainFlagSet { get; set; }

        public object UnparsableValue { get; set; }

        public bool IsDuplicatedFlagSet { get; set; }
    }

    public class Subscription
    {
        public Subscription(string topic, Action<ReceivedMessageArguments> receivedMessageAction, Qos qoSS)
        {
            Topic = topic;
            ReceivedMessageAction = receivedMessageAction;
            Qos = qoSS;

            if (topic.Contains("+") || topic.Contains("#"))
            {
                _regexTopic = topic.Replace("+", "[^/]+").Replace("#", ".*");
            }
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj.GetType() != GetType())
            {
                return false;
            }
            return Equals((Subscription)obj);
        }

        public override int GetHashCode()
        {
            return Topic?.GetHashCode() ?? 0;
        }

        public override string ToString()
        {
            return Topic;
        }

        /// <summary>
        /// return true if a concrete topic matches the generic topic with place holders
        /// e.g. xy/mgw/monitoring/y/z matches topic +/mgw/monitoring/#
        /// </summary>
        /// <param name="concreteTopic"></param>
        /// <returns></returns>
        public bool IsMatchingTopic(string concreteTopic)
        {
            return _regexTopic != null ? Regex.IsMatch(concreteTopic, _regexTopic) : Topic.Equals(concreteTopic);
        }

        private bool Equals(Subscription other)
        {
            return string.Equals(Topic, other.Topic);
        }

        public string Topic { get; }

        public Qos Qos { get; }

        public Action<ReceivedMessageArguments> ReceivedMessageAction { get; }

        /// <summary>
        /// Id will be set after one try to send. It is null, if the client is not connected and so no publish has been done
        /// </summary>
        internal ushort? SentId { get; set; }

        /// <summary>
        /// Set, if subscription with MQTT wildcards
        /// </summary>
        private readonly string _regexTopic;
    }
}
