using System;
using System.Collections.Generic;
using System.Linq;
using NexusEnroll.Patterns;

namespace NexusEnroll.Services
{

    public interface INotificationService
    {
        void AddObserver(INotificationObserver observer);
        void RemoveObserver(INotificationObserver observer);
        void NotifyEvent(string eventType, IDictionary<string, object> data = null);
        IReadOnlyList<NotificationEvent> GetHistory();
    }


    public class NotificationService : INotificationService, ISubject
    {
        private readonly List<INotificationObserver> _observers = new List<INotificationObserver>();
        private readonly List<NotificationEvent> _history = new List<NotificationEvent>();

        public void Attach(INotificationObserver observer) => AddObserver(observer);
        public void Detach(INotificationObserver observer) => RemoveObserver(observer);

        public void AddObserver(INotificationObserver observer)
        {
            if (observer == null) throw new ArgumentNullException(nameof(observer));
            if (!_observers.Contains(observer))
                _observers.Add(observer);
        }

        public void RemoveObserver(INotificationObserver observer)
        {
            if (observer == null) return;
            _observers.Remove(observer);
        }

        public void NotifyEvent(string eventType, IDictionary<string, object> data = null)
        {
            var notificationEvent = new NotificationEvent(eventType, data);
            Notify(notificationEvent);
        }

        public void Notify(NotificationEvent notificationEvent)
        {
            _history.Add(notificationEvent);

            // Copy to array so an observer removing itself during Update()
            // can't invalidate the enumeration.
            foreach (var observer in _observers.ToArray())
            {
                try
                {
                    observer.Update(notificationEvent);
                }
                catch (Exception ex)
                {
                    // triggering broken observers - log and move on to the next observer.
                    Console.WriteLine($"[NotificationService] Observer '{observer.ObserverName}' failed: {ex.Message}");
                }
            }
        }

        public IReadOnlyList<NotificationEvent> GetHistory() => _history.AsReadOnly();
    }
}
