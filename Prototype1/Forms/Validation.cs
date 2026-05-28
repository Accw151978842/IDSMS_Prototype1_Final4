using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Prototype1.Forms
{
    /// <summary>
    /// Generic input validation helper.
    /// Usage:
    ///   var v = new Validation(this);
    ///   v.Required(txtName, "Name is required.");
    ///   v.Regex(txtEmail, @"^[\w\.\-]+@[\w\-]+\.[A-Za-z]{2,}$", "Invalid email.");
    ///   v.Money(txtPrice, "Price must be >= 0.");
    ///   if (!v.ValidateAll()) return;        // shows red icons + tooltip
    /// </summary>
    public class Validation
    {
        private readonly ContainerControl host;
        private readonly ErrorProvider provider;
        private readonly List<Func<bool>> rules = new List<Func<bool>>();

        // ---------- common regex patterns ----------
        public const string RxEmail        = @"^[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}$";
        public const string RxHkPhone      = @"^(\+?\d{1,3}[\s\-]?)?\d{8,15}$";   // tolerant: HK 8-digit + intl
        public const string RxHkVehicle    = @"^[A-Za-z]{1,3}\s?\d{1,4}[A-Za-z]?$"; // e.g. LV 1234, AB1234A
        public const string RxUsername     = @"^[A-Za-z0-9_]{3,20}$";
        public const string RxLettersOnly  = @"^[A-Za-z\u4e00-\u9fa5\s\.\-]{2,50}$"; // English + Chinese name

        public Validation(ContainerControl owner)
        {
            host = owner;
            provider = new ErrorProvider(owner)
            {
                BlinkStyle = ErrorBlinkStyle.NeverBlink
            };
        }

        // ---------- rule builders (chainable through Add(...)) ----------

        public Validation Required(Control c, string message)
        {
            rules.Add(() =>
            {
                string text = GetText(c);
                if (string.IsNullOrWhiteSpace(text))
                {
                    Set(c, message);
                    return false;
                }
                Clear(c);
                return true;
            });
            return this;
        }

        public Validation MinLength(Control c, int min, string message)
        {
            rules.Add(() =>
            {
                string text = GetText(c) ?? "";
                if (text.Trim().Length < min) { Set(c, message); return false; }
                Clear(c); return true;
            });
            return this;
        }

        public Validation MaxLength(Control c, int max, string message)
        {
            rules.Add(() =>
            {
                string text = GetText(c) ?? "";
                if (text.Trim().Length > max) { Set(c, message); return false; }
                Clear(c); return true;
            });
            return this;
        }

        public Validation Regex(Control c, string pattern, string message)
        {
            var rx = new Regex(pattern);
            rules.Add(() =>
            {
                string text = GetText(c) ?? "";
                if (string.IsNullOrWhiteSpace(text)) { Clear(c); return true; } // let Required handle empty
                if (!rx.IsMatch(text.Trim())) { Set(c, message); return false; }
                Clear(c); return true;
            });
            return this;
        }

        public Validation Email(Control c, string message = "Invalid email format.")
            => Regex(c, RxEmail, message);

        public Validation Phone(Control c, string message = "Invalid phone number.")
            => Regex(c, RxHkPhone, message);

        public Validation Money(Control c, string message = "Must be a number >= 0.")
        {
            rules.Add(() =>
            {
                string text = (GetText(c) ?? "").Trim();
                if (string.IsNullOrWhiteSpace(text)) { Clear(c); return true; }
                decimal v;
                if (!decimal.TryParse(text, out v) || v < 0)
                { Set(c, message); return false; }
                Clear(c); return true;
            });
            return this;
        }

        public Validation Integer(Control c, int min, int max, string message)
        {
            rules.Add(() =>
            {
                string text = (GetText(c) ?? "").Trim();
                if (string.IsNullOrWhiteSpace(text)) { Clear(c); return true; }
                int v;
                if (!int.TryParse(text, out v) || v < min || v > max)
                { Set(c, message); return false; }
                Clear(c); return true;
            });
            return this;
        }

        public Validation Selected(ComboBox cmb, string message)
        {
            rules.Add(() =>
            {
                if (cmb.SelectedItem == null && string.IsNullOrWhiteSpace(cmb.Text))
                { Set(cmb, message); return false; }
                Clear(cmb); return true;
            });
            return this;
        }

        /// <summary>Custom predicate. Return true if valid.</summary>
        public Validation Custom(Control c, Func<bool> predicate, string message)
        {
            rules.Add(() =>
            {
                if (!predicate()) { Set(c, message); return false; }
                Clear(c); return true;
            });
            return this;
        }

        // ---------- run all rules ----------

        public bool ValidateAll()
        {
            // Run every rule (don't short-circuit, so user sees ALL errors at once)
            bool ok = true;
            foreach (var r in rules)
            {
                if (!r()) ok = false;
            }
            if (!ok)
            {
                // Beep + focus first error
                System.Media.SystemSounds.Beep.Play();
            }
            return ok;
        }

        public void Reset()
        {
            provider.Clear();
        }

        // ---------- key-press filters (use directly on TextBox.KeyPress) ----------

        /// <summary>Only digits + decimal point allowed.</summary>
        public static void OnlyDecimal(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar)) return;
            if (char.IsDigit(e.KeyChar)) return;
            var tb = sender as TextBox;
            if (e.KeyChar == '.' && tb != null && !tb.Text.Contains(".")) return;
            e.Handled = true;
        }

        /// <summary>Only digits allowed.</summary>
        public static void OnlyDigits(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar) || char.IsDigit(e.KeyChar)) return;
            e.Handled = true;
        }

        /// <summary>Letters + digits + space (for vehicle plates etc).</summary>
        public static void OnlyAlphanumeric(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar) || char.IsLetterOrDigit(e.KeyChar) || e.KeyChar == ' ')
                return;
            e.Handled = true;
        }

        // ---------- internal helpers ----------

        private string GetText(Control c)
        {
            if (c is TextBox)  return ((TextBox)c).Text;
            if (c is ComboBox) return ((ComboBox)c).Text;
            return c.Text;
        }

        private void Set(Control c, string message)
        {
            provider.SetError(c, message);
        }

        private void Clear(Control c)
        {
            provider.SetError(c, "");
        }
    }
}
