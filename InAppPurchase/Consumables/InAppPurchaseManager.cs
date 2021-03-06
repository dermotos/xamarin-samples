using System;
using System.Collections.Generic;
using MonoTouch.StoreKit;
using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace Consumables {
	public class InAppPurchaseManager : SKProductsRequestDelegate {
		public static NSString InAppPurchaseManagerProductsFetchedNotification = new NSString("InAppPurchaseControllerProductsFetchedNotification");
		public static NSString InAppPurchaseManagerTransactionFailedNotification  = new NSString("InAppPurchaseManagerTransactionFailedNotification");
		public static NSString InAppPurchaseManagerTransactionSucceededNotification  = new NSString("InAppPurchaseManagerTransactionSucceededNotification");
		
		SKProductsRequest productsRequest;
		CustomPaymentObserver theObserver;

		public static NSAction Done {get;set;}

		public InAppPurchaseManager ()
		{
			theObserver = new CustomPaymentObserver(this);
			
			// Call this once upon startup of in-app-purchase activities
			// This also kicks off the TransactionObserver which handles the various communications
			SKPaymentQueue.DefaultQueue.AddTransactionObserver(theObserver);
		}
		
		// Verify that the iTunes account can make this purchase for this application
		public bool CanMakePayments()
		{
			return SKPaymentQueue.CanMakePayments;	
		}

		// request multiple products at once
		public void RequestProductData (List<string> productIds)
		{
			var array = new NSString[productIds.Count];
			for (var i = 0; i < productIds.Count; i++) {
				array[i] = new NSString(productIds[i]);
			}
		 	NSSet productIdentifiers = NSSet.MakeNSObjectSet<NSString>(array);			

			//set up product request for in-app purchase
			productsRequest  = new SKProductsRequest(productIdentifiers);
			productsRequest.Delegate = this; // SKProductsRequestDelegate.ReceivedResponse
			productsRequest.Start();
		}

		public override void ReceivedResponse (SKProductsRequest request, SKProductsResponse response)
		{
			SKProduct[] products = response.Products;

			NSDictionary userInfo = null;
			if (products.Length > 0) {
				NSObject[] productIdsArray = new NSObject[response.Products.Length];
				NSObject[] productsArray = new NSObject[response.Products.Length];
				for (int i = 0; i < response.Products.Length; i++) {
					productIdsArray[i] = new NSString(response.Products[i].ProductIdentifier);
					productsArray[i] = response.Products[i];
				}
				userInfo = NSDictionary.FromObjectsAndKeys (productsArray, productIdsArray);
			}
			NSNotificationCenter.DefaultCenter.PostNotificationName(InAppPurchaseManagerProductsFetchedNotification,this,userInfo);

			foreach (string invalidProductId in response.InvalidProducts) {
				Console.WriteLine("Invalid product id: " + invalidProductId );
			}
		}


		public void PurchaseProduct(string appStoreProductId)
		{
			Console.WriteLine("PurchaseProduct " + appStoreProductId);
			SKPayment payment = SKPayment.PaymentWithProduct (appStoreProductId);	
			SKPaymentQueue.DefaultQueue.AddPayment (payment);
		}
		public void CompleteTransaction (SKPaymentTransaction transaction)
		{
			Console.WriteLine("CompleteTransaction " + transaction.TransactionIdentifier);
			var productId = transaction.Payment.ProductIdentifier;
			if (productId == ConsumableViewController.Buy5ProductId)
				CreditManager.Add(5);
			else if (productId == ConsumableViewController.Buy10ProductId)
				CreditManager.Add(10);
			else
				Console.WriteLine ("Shouldn't happen, there are only two products");
			
			FinishTransaction(transaction, true);
		}
		public void FailedTransaction (SKPaymentTransaction transaction)
		{
			//SKErrorPaymentCancelled == 2
			if (transaction.Error.Code == 2) // user cancelled
				Console.WriteLine("User CANCELLED FailedTransaction Code=" + transaction.Error.Code + " " + transaction.Error.LocalizedDescription);
			else // error!
				Console.WriteLine("FailedTransaction Code=" + transaction.Error.Code + " " + transaction.Error.LocalizedDescription);
			
			FinishTransaction(transaction,false);
		}
		public void FinishTransaction(SKPaymentTransaction transaction, bool wasSuccessful)
		{
			Console.WriteLine("FinishTransaction " + wasSuccessful);
			// remove the transaction from the payment queue.
			SKPaymentQueue.DefaultQueue.FinishTransaction(transaction);		// THIS IS IMPORTANT - LET'S APPLE KNOW WE'RE DONE !!!!
			
			using (var pool = new NSAutoreleasePool()) {
				NSDictionary userInfo = NSDictionary.FromObjectsAndKeys(new NSObject[] {transaction},new NSObject[] { new NSString("transaction")});
				if (wasSuccessful) {
					// send out a notification that we’ve finished the transaction
					NSNotificationCenter.DefaultCenter.PostNotificationName(InAppPurchaseManagerTransactionSucceededNotification,this,userInfo);
				} else {
					// send out a notification for the failed transaction
					NSNotificationCenter.DefaultCenter.PostNotificationName(InAppPurchaseManagerTransactionFailedNotification,this,userInfo);
				}
			}
		}

		/// <summary>
		/// Probably could not connect to the App Store (network unavailable?)
		/// </summary>
		public override void RequestFailed (SKRequest request, NSError error)
		{
			Console.WriteLine (" ** InAppPurchaseManager RequestFailed() " + error.LocalizedDescription);
		}
	}
}