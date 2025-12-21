
using System;
using System.ComponentModel;
using System.IO.Compression;
using System.Runtime.CompilerServices;

public class PaymentRequest
{
  public string Sender { get; set; }
  public string Receiver { get; set; }
  public int Amount { get; set; }
  public string Currency { get; set; }

  public PaymentRequest(string sender, string receiver, int amount, string currency)
  {
    Sender = sender;
    Receiver = receiver;
    Amount = amount;
    Currency = currency;
  }
}

public enum GateWayType
{
  Paytm,
  RazorPay
}


public interface IBankingSystem
{
  public bool ProcessPayment(PaymentRequest req);
}


public class PaytmBakingSystem : IBankingSystem
{
  private static Random rand = new Random();
  public bool ProcessPayment(PaymentRequest req)
  {
    //Simulating 20% success rate
    return rand.Next(0, 100) <=20; // Simulate success/failure
  }
}

public class RazorpayBankingSystem: IBankingSystem
{
  //70% Success rate
  private Random rand = new Random();
  public bool ProcessPayment(PaymentRequest req)
  {
    //Simulating 10% success rate
    return rand.Next(0, 100) <=70; // Simulate success/failure
  }
}
/// <summary>
/// Abstract class for Payment Gateway (Template design pattern)
/// </summary>
public abstract class PaymentGateway
{
  public IBankingSystem? _bankingSystem;
  public PaymentGateway()
  {
    _bankingSystem = null;
  }
  public virtual bool ProcessPayment(PaymentRequest req)
  {
    if (!validatePaymentRequest(req))
    {
      Console.WriteLine($"payment validation failed for {req.Sender} .");
      return false;
    }
    if (!initiatePayment(req))
    {
      Console.WriteLine($"Payment initiate failed for {req.Sender}.");
      return false;
    }

    if (!confirmpayment(req))
    {
       Console.WriteLine($"Payment Confirmation failed for {req.Sender}.");
      return false;
    }
    return true;
  }

  public abstract bool validatePaymentRequest(PaymentRequest req);
  public abstract bool initiatePayment(PaymentRequest req);
  public abstract bool confirmpayment(PaymentRequest req);

}


public class PaytmPaymentGateway : PaymentGateway
{
  public PaytmPaymentGateway()
  {
    this._bankingSystem = new PaytmBakingSystem();
  }
  public override bool validatePaymentRequest(PaymentRequest req)
  {

    Console.WriteLine($"Validting Payment Request for :{req.Sender}");
    if (req.Amount <= 0 && "INR".Equals(req.Currency))
    {
      return false;
    }
    return true;
  }

  public override bool initiatePayment(PaymentRequest req)
  {
    Console.WriteLine($"Initiating Payment for :{req.Sender} to: {req.Receiver} Amount: {req.Amount} {req.Currency}");
    if (_bankingSystem.ProcessPayment(req))
    {
      Console.WriteLine($"Payment Initiated successfully for :{req.Sender}");
      return true;
    }
    return false;
  }

  public override bool confirmpayment(PaymentRequest req)
  {
    Console.WriteLine("[Paytm] Confirming payment for " + req.Sender + ".");
    return true;
  }
}


/// <summary>
/// Razorpay Payment Gateway
/// </summary>



public class RazorPayPaymentGateway : PaymentGateway
{
  public RazorPayPaymentGateway()
  {
    this._bankingSystem = new RazorpayBankingSystem();
  }
  public override bool validatePaymentRequest(PaymentRequest req)
  {

    Console.WriteLine($"Validting Payment Request for :{req.Sender}");
    if (req.Amount <= 0 && "INR".Equals(req.Currency))
    {
      return false;
    }
    return true;
  }

  public override bool initiatePayment(PaymentRequest req)
  {
    Console.WriteLine($"Initiating Payment for :{req.Sender} to: {req.Receiver} Amount: {req.Amount} {req.Currency}");
    if (_bankingSystem.ProcessPayment(req))
    {
      Console.WriteLine($"Payment Initiated successfully for :{req.Sender}");
      return true;
    }
    return false;
  }

  public override bool confirmpayment(PaymentRequest req)
  {
    Console.WriteLine("[RazorPay] Confirming payment for " + req.Sender + ".");
    return true;
  }
}



/// <summary>
/// Proxy class for Payment Service
/// </summary>
public class PaymentGatewayProxy : PaymentGateway
{
  private PaymentGateway _realPaymentGateway;

  private int _maxRetries;

  public PaymentGatewayProxy(PaymentGateway pg, int maxRetries)
  {
    _realPaymentGateway = pg;
    _maxRetries = maxRetries;
  }

  public override bool ProcessPayment(PaymentRequest paymentRequest)
  {
    bool result = false;
    for (int attempt = 0; attempt < _maxRetries; attempt++)
    {
      if (attempt > 0)
      {
        Console.WriteLine($"[Proxy] Retrying Payment (attempt: {attempt})");
      }

      result = _realPaymentGateway.ProcessPayment(paymentRequest);
      if (result)
      {
        Console.WriteLine("[Proxy] Payment succeeded for " + paymentRequest.Sender + " on attempt " + (attempt + 1) + ".");
        break;
      }
      if (!result)
      {
        Console.WriteLine("[Proxy] Payment failed after " + attempt
                + " attempts for " + paymentRequest.Sender + ".");
      }
      return result;
    }
    return false;
  }

  public override bool validatePaymentRequest(PaymentRequest req)
  {
    return _realPaymentGateway.validatePaymentRequest(req);
  }

  public override bool initiatePayment(PaymentRequest req)
  {
    return _realPaymentGateway.initiatePayment(req);
  }

  public override bool confirmpayment(PaymentRequest req)
  {
    return _realPaymentGateway.confirmpayment(req);
  }
}

public class GatewayFactory
{
  private GatewayFactory(){}
  private readonly static Lazy<GatewayFactory> factoryInstance = new Lazy<GatewayFactory>(() => new GatewayFactory());
  public static GatewayFactory getInstance = factoryInstance.Value;
  public PaymentGateway GetPaymentGatewayInstance(GateWayType type){
    if (GateWayType.RazorPay == type)
    {
      PaymentGateway paymentGateway = new PaytmPaymentGateway();
      return new PaymentGatewayProxy(paymentGateway, 3);
    }
    else if (GateWayType.Paytm == type)
    {
      PaymentGateway paymentGateway = new RazorPayPaymentGateway();
      return new PaymentGatewayProxy(paymentGateway, 3);
    }
    return new PaymentGatewayProxy(new PaytmPaymentGateway(), 3);
  }
}


public class PaymentService
{
  public PaymentGateway _paymentGateway;
  private PaymentService(){}
  public static readonly Lazy<PaymentService> instance =
  new Lazy<PaymentService>(() => new PaymentService());

  public static PaymentService getInstance = instance.Value;

  public void SetGateway(PaymentGateway pg)
  {
    _paymentGateway = pg;
  }

  public bool ProcessPayment(PaymentRequest pr)
  {
    if (_paymentGateway == null)
    {
      throw new Exception("Payment Gateway not set.");
    }
    return _paymentGateway.ProcessPayment(pr);
  }
}

public class PaymentController
{
  private PaymentController(){}
  private readonly static Lazy<PaymentController> paymentController = new Lazy<PaymentController>(() => new PaymentController());
  public static PaymentController getInstance = paymentController.Value;
  public bool handlePayment(GateWayType gt, PaymentRequest request)
  {
    PaymentGateway pg = GatewayFactory.getInstance.GetPaymentGatewayInstance(gt);
    PaymentService.getInstance.SetGateway(pg);
    return PaymentService.getInstance.ProcessPayment(request);
  }
}

public class Program
{
  public static void Main(string[] args)
  {
    var newPaymentRequest = new PaymentRequest("Alice", "Bob", 1000, "INR");
    var paymentController = PaymentController.getInstance;
    bool paymentStatus = paymentController.handlePayment(GateWayType.Paytm, newPaymentRequest);
    Console.WriteLine($"Payment Status: {paymentStatus}");  
  }
}