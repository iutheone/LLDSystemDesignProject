

using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Xml.Serialization;


public class NotificationSystem{


  /// <summary>
  /// Notifcation Decorators
  /// </summary>
  public interface INotification
  {
    public string GetContent();
  }

  public class SimpleNotification : INotification
  {
    public string _content;
    public SimpleNotification(string content)
    {
      _content = content;
    }
    public string GetContent()
    {
      return _content;
    }
  }

  public abstract class NotificationDecorator : INotification
  {
    public INotification _notification;
    public NotificationDecorator(INotification notification)
    {
      _notification = notification;
    }
    public virtual string GetContent()
    {
      return _notification.GetContent();
    }
  }


  public class TimeStampNotificationDecorator : NotificationDecorator
  {
    public TimeStampNotificationDecorator(INotification notification) : base(notification) { }
    public override string GetContent()
    {
      return $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} - {_notification.GetContent()}";
    }
  }


  public class SignatureNotificationDecorator : NotificationDecorator
  {
    public SignatureNotificationDecorator(INotification notification) : base(notification) { }
    public override string GetContent()
    {
      return "Sent By Admin(sign)" + $"{_notification.GetContent()}";
    }
  }


  public interface IObserver
  {
    public void Update();
  }

  public interface IObservable
  {
    public void Add(IObserver observer);
    public void Remove(IObserver observer);

    public void NotifyAllObserver();
  }



  public class NotificatinObservable : IObservable
  {
    private List<IObserver> _observers = new List<IObserver>();
    public INotification currentNotification;

    public void Add(IObserver observer)
    {
      _observers.Add(observer);
    }

    public void Remove(IObserver observer)
    {
      _observers.Remove(observer);
    }

    public void NotifyAllObserver()
    {
      foreach (var observer in _observers)
      {
        observer.Update();
      }
    }

    public void sendNotification(INotification notification)
    {
      currentNotification = notification;
      NotifyAllObserver();
    }

    public string GetNotificationContent()
    {
      return currentNotification.GetContent();
    }
  }


  public class NotificationService
  {
    public NotificatinObservable notificatinObservable;

    private static readonly Lazy<NotificationService> _instance = new Lazy<NotificationService>(() => new NotificationService(notificatinObservable: new NotificatinObservable()));

    public static NotificationService Instance = _instance.Value;
    public NotificationService(NotificatinObservable notificatinObservable)
    {
      this.notificatinObservable = notificatinObservable;
    }

    private IList<INotification> _notifications = new List<INotification>();

    public void sendNotification(INotification notification)
    {
      _notifications.Add(notification);
      notificatinObservable.sendNotification(notification);
    }
  }



  public class Logger : IObserver
  {
    private NotificatinObservable _notificatinObservable;

    public Logger()
    {
      this._notificatinObservable = NotificationService.Instance.notificatinObservable;
    }

    public Logger(NotificatinObservable notificatinObservable)
    {
      _notificatinObservable = notificatinObservable;
      _notificatinObservable.Add(this);
    }

    public void Update()
    {
      Console.WriteLine($"Logging new Notification :\n {_notificatinObservable.currentNotification.GetContent()}");
    }
  }

  /*============================
    Strategy Pattern Components (Concrete Observer 2)
  =============================*/

  // Abstract class for different Notification Strategies.
  public interface INotificationStrategy
  {
    public void sendNotification(string content);
  };


  public class EmailNotificationStrategy : INotificationStrategy
  {
    private string _emailAddress;
    public EmailNotificationStrategy(string emailAddress)
    {
      _emailAddress = emailAddress;
    }
    public void sendNotification(string content)
    {
      Console.WriteLine($"Sending Email Notification to emailID : {_emailAddress} with content: {content}");
    }
  }

  public class SMSNotificationStrategy : INotificationStrategy
  {
    private string _phoneNumber;
    public SMSNotificationStrategy(string phoneNumber)
    {
      _phoneNumber = phoneNumber;
    }
    public void sendNotification(string content)
    {
      Console.WriteLine($"Sending SMS Notification to Phone Number : {_phoneNumber} with content: {content}");
    }
  }


  public class NotificationEngine : IObserver
  {
    private NotificatinObservable _notificatinObservable;
    private List<INotificationStrategy> _notificationStrategies = new List<INotificationStrategy>();

    public NotificationEngine()
    {
      this._notificatinObservable = NotificationService.Instance.notificatinObservable;
      _notificatinObservable.Add(this);
    }

    public void AddNotifcationStrategy(INotificationStrategy notificationStrategy)
    {
      _notificationStrategies.Add(notificationStrategy);
    }

    public void Update()
    {
      string content = _notificatinObservable.GetNotificationContent();
      foreach (var strategy in _notificationStrategies)
      {
        strategy.sendNotification(content);
      }
    }
  }

  static void Main(string[] args){
    // Create NotificationService.
    NotificationService notificationService=  NotificationService.Instance;
     
      // Create Observers
      Logger logger = new Logger();
      NotificationEngine notificationEngine = new NotificationEngine();
    Console.WriteLine("Observers Registered");

    notificationEngine.AddNotifcationStrategy(new EmailNotificationStrategy("dhawanayu@gmail.com"));
    notificationEngine.AddNotifcationStrategy(new SMSNotificationStrategy("+7983168691"));
    INotification notification1 = new SimpleNotification("This is a simple notification");
    notificationService.sendNotification(notification1);
    notification1 = new TimeStampNotificationDecorator(notification1);
    notification1 = new SignatureNotificationDecorator(notification1);
    notificationService.sendNotification(notification1);
  }
}

