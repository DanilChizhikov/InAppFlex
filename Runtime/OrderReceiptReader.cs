using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Purchasing;

namespace DTech.InAppFlex
{
    internal static class OrderReceiptReader
    {
        private const string GooglePlayStoreName = "GooglePlay";
        private const string NoReference = "none";
        private const int ReferenceBytes = 8;

        public static string GetTransactionId(Order order)
        {
            string transactionId = order?.Info?.TransactionID ?? string.Empty;
            GooglePlayPurchase purchase = ReadGooglePlayPurchase(order?.Info?.Receipt);
            return string.IsNullOrEmpty(purchase?.orderId) ? transactionId : purchase.orderId;
        }

        public static int GetQuantity(Order order, CartItem cartItem)
        {
            int cartQuantity = cartItem != null && cartItem.Quantity > 0 ? cartItem.Quantity : 1;
            GooglePlayPurchase purchase = ReadGooglePlayPurchase(order?.Info?.Receipt);
            return purchase != null && purchase.quantity > 0 ? purchase.quantity : cartQuantity;
        }

        public static string GetSignedTransaction(Order order)
        {
            string signedTransaction = order?.Info?.Apple?.jwsRepresentation;
            return string.IsNullOrEmpty(signedTransaction) ? string.Empty : signedTransaction;
        }

        public static string GetLogReference(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId))
            {
                return NoReference;
            }

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(transactionId));
                var builder = new StringBuilder("sha256:");
                for (int i = 0; i < ReferenceBytes; i++)
                {
                    builder.Append(hash[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }

        private static GooglePlayPurchase ReadGooglePlayPurchase(string receipt)
        {
            if (string.IsNullOrEmpty(receipt))
            {
                return null;
            }

            try
            {
                UnifiedReceipt unifiedReceipt = JsonUtility.FromJson<UnifiedReceipt>(receipt);
                if (unifiedReceipt == null || unifiedReceipt.Store != GooglePlayStoreName || string.IsNullOrEmpty(unifiedReceipt.Payload))
                {
                    return null;
                }

                GooglePlayPayload payload = JsonUtility.FromJson<GooglePlayPayload>(unifiedReceipt.Payload);
                return payload == null || string.IsNullOrEmpty(payload.json)
                        ? null
                        : JsonUtility.FromJson<GooglePlayPurchase>(payload.json);
            }
            catch (Exception)
            {
                return null;
            }
        }

        [Serializable]
        private sealed class UnifiedReceipt
        {
            public string Store;
            public string Payload;
        }

        [Serializable]
        private sealed class GooglePlayPayload
        {
            public string json;
        }

        [Serializable]
        private sealed class GooglePlayPurchase
        {
            public string orderId;
            public int quantity;
        }
    }
}