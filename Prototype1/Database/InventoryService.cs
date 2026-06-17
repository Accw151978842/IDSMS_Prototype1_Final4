using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Prototype1.Models;

namespace Prototype1.Database
{
    /// <summary>
    /// Centralises stock movements that are driven by the sales-order
    /// lifecycle.
    ///
    /// Rule: stock is deducted when an order first reaches a "shipped"
    /// state (Shipped or Completed), and restored if such an order is later
    /// cancelled. The <see cref="SalesOrder.StockDeducted"/> flag guarantees
    /// each order deducts exactly once and restores exactly once, no matter
    /// how many times it is saved in between.
    /// </summary>
    public static class InventoryService
    {
        /// <summary>Statuses that represent goods having left the warehouse.</summary>
        public static bool IsShippedStatus(string status)
        {
            return string.Equals(status, "Shipped",    StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "Completed",  StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Check whether there is enough stock to ship every line on the order.
        /// Returns true when OK; otherwise returns false and fills
        /// <paramref name="message"/> with a human-readable shortage list.
        /// </summary>
        public static bool CanFulfill(SalesOrder order, out string message)
        {
            message = "";
            if (order == null) return true;

            // Sum requested quantity per item (an order may list an item twice).
            var needed = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var ln in order.Lines)
            {
                if (string.IsNullOrEmpty(ln.ItemId)) continue;
                if (needed.ContainsKey(ln.ItemId)) needed[ln.ItemId] += ln.Quantity;
                else needed[ln.ItemId] = ln.Quantity;
            }

            var shortages = new StringBuilder();
            foreach (var kv in needed)
            {
                var item = DataStore.Items.FirstOrDefault(i => i.ItemId == kv.Key);
                if (item == null)
                {
                    shortages.AppendLine("- " + kv.Key + " (item not found)");
                    continue;
                }
                if (item.StockQty < kv.Value)
                {
                    shortages.AppendLine("- " + item.ItemName + " (" + item.ItemId +
                        "): need " + kv.Value + ", in stock " + item.StockQty);
                }
            }

            if (shortages.Length > 0)
            {
                message = "Not enough stock to ship this order:\r\n" + shortages.ToString().TrimEnd();
                return false;
            }
            return true;
        }

        /// <summary>
        /// Apply the correct stock movement for the order's new status.
        ///
        /// - Moving INTO a shipped state for the first time deducts stock and
        ///   sets StockDeducted = true.
        /// - Moving OUT of a shipped state (e.g. back to Pending, or to
        ///   Cancelled) while stock was deducted restores the stock and clears
        ///   the flag.
        /// - Saving a shipped order again (Shipped -> Completed) does nothing,
        ///   because StockDeducted is already true.
        ///
        /// Caller is responsible for persisting via DataStore.SaveAll().
        /// </summary>
        public static void ApplyStatusChange(SalesOrder order)
        {
            if (order == null) return;

            bool shouldBeDeducted = IsShippedStatus(order.Status)
                && !string.Equals(order.Status, "Cancelled", StringComparison.OrdinalIgnoreCase);

            if (shouldBeDeducted && !order.StockDeducted)
            {
                AdjustStock(order, deduct: true);
                order.StockDeducted = true;
            }
            else if (!shouldBeDeducted && order.StockDeducted)
            {
                AdjustStock(order, deduct: false);
                order.StockDeducted = false;
            }
        }

        /// <summary>
        /// Restore stock for an order that is being cancelled, if it had been
        /// deducted. Safe to call on any order. Caller persists afterwards.
        /// </summary>
        public static void RestoreOnCancel(SalesOrder order)
        {
            if (order == null) return;
            if (order.StockDeducted)
            {
                AdjustStock(order, deduct: false);
                order.StockDeducted = false;
            }
        }

        // Adds (deduct=false) or subtracts (deduct=true) each line quantity
        // from the matching item's stock. Stock is clamped at zero so a data
        // inconsistency can never push it negative.
        private static void AdjustStock(SalesOrder order, bool deduct)
        {
            foreach (var ln in order.Lines)
            {
                if (string.IsNullOrEmpty(ln.ItemId)) continue;
                var item = DataStore.Items.FirstOrDefault(i => i.ItemId == ln.ItemId);
                if (item == null) continue;

                if (deduct)
                {
                    item.StockQty -= ln.Quantity;
                    if (item.StockQty < 0) item.StockQty = 0;
                }
                else
                {
                    item.StockQty += ln.Quantity;
                }
            }
        }
    }
}
