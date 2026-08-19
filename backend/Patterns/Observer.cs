using System;
using System.Collections.Generic;

namespace NexusEnroll.Patterns
{

    public class NotificationEvent
    {
        public string EventType { get; }
        public DateTime Timestamp { get; }
        public IReadOnlyDictionary<string, object> Data { get; }

        public NotificationEvent(string eventType, IDictionary<string, object> data = null)
        {
            if (string.IsNullOrWhiteSpace(eventType))
                throw new ArgumentException("EventType is required.", nameof(eventType));

            EventType = eventType;
            Timestamp = DateTime.UtcNow;
            Data = new Dictionary<string, object>(data ?? new Dictionary<string, object>());
        }

        public object Get(string key) => Data.TryGetValue(key, out var v) ? v : null;

        public override string ToString()
            => $"[{Timestamp:u}] {EventType} ({Data.Count} field(s))";
    }

    public interface INotificationObserver
    {
        string ObserverName { get; }
        void Update(NotificationEvent notificationEvent);
    }

    public interface ISubject
    {
        void Attach(INotificationObserver observer);
        void Detach(INotificationObserver observer);
        void Notify(NotificationEvent notificationEvent);
    }

    public abstract class NotificationObserverBase : INotificationObserver
    {
        public string ObserverName { get; }

        protected NotificationObserverBase(string observerName)
        {
            ObserverName = observerName;
        }

        public abstract void Update(NotificationEvent notificationEvent);
    }


    public class ConsoleNotificationObserver : NotificationObserverBase
    {
        public ConsoleNotificationObserver() : base("ConsoleObserver") { }

        public override void Update(NotificationEvent notificationEvent)
        {
            Console.WriteLine($"[CONSOLE] {notificationEvent}");
            foreach (var kv in notificationEvent.Data)
                Console.WriteLine($"    - {kv.Key}: {kv.Value}");
        }
    }


    public class EmailNotificationObserver : NotificationObserverBase
    {
        public EmailNotificationObserver() : base("EmailObserver") { }

        public override void Update(NotificationEvent notificationEvent)
        {
            var recipient = notificationEvent.Get("RecipientEmail")?.ToString() ?? "unknown@nexus.edu";
            Console.WriteLine($"[EMAIL -> {recipient}] Subject: {notificationEvent.EventType}");
        }
    }

    /// <summary>
    /// Simulated SMS channel.
    /// </summary>
    public class SmsNotificationObserver : NotificationObserverBase
    {
        public SmsNotificationObserver() : base("SmsObserver") { }

        public override void Update(NotificationEvent notificationEvent)
        {
            var phone = notificationEvent.Get("RecipientPhone")?.ToString() ?? "unknown";
            Console.WriteLine($"[SMS -> {phone}] {notificationEvent.EventType}");
        }
    }
}
