using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Prototype1.Models;

namespace Prototype1.Database
{
    public static class SecurityService
    {
        public static User CurrentUser { get; private set; }

        // ---------- Session tracking ----------
        public static DateTime LastActivityAt { get; private set; }
        /// <summary>Total minutes of inactivity allowed before auto-logout.</summary>
        public const int SessionTimeoutMinutes = 15;
        /// <summary>How many minutes before timeout the warning popup appears.</summary>
        public const int SessionWarnBeforeMinutes = 2;

        /// <summary>Reset the idle timer (call on every user interaction).</summary>
        public static void TouchActivity() { LastActivityAt = DateTime.Now; }

        /// <summary>Seconds left before auto-logout. Negative if already expired.</summary>
        public static int SecondsLeft()
        {
            if (CurrentUser == null) return SessionTimeoutMinutes * 60;
            var elapsed = (DateTime.Now - LastActivityAt).TotalSeconds;
            return (int)Math.Max(-1, (SessionTimeoutMinutes * 60) - elapsed);
        }

        public static string Hash(string input)
        {
            if (input == null) input = string.Empty;
            using (var sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
                var sb = new StringBuilder();
                foreach (var b in bytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }

        public static bool Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || password == null)
            {
                return false;
            }
            string hash = Hash(password);
            var user = DataStore.Users.FirstOrDefault(u =>
                string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase) &&
                u.PasswordHash == hash &&
                u.Active);
            if (user == null)
            {
                Audit(username, "Login Failed", "Invalid credentials");
                return false;
            }
            CurrentUser = user;
            LastActivityAt = DateTime.Now;
            Audit(user.Username, "Login", "User logged in");
            DataStore.SaveAll();
            return true;
        }

        public static void Logout()
        {
            if (CurrentUser != null)
            {
                Audit(CurrentUser.Username, "Logout", "User logged out");
                DataStore.SaveAll();
            }
            CurrentUser = null;
        }

        public static bool ChangePassword(string username, string oldPassword, string newPassword)
        {
            var user = DataStore.Users.FirstOrDefault(u =>
                string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));
            if (user == null || user.PasswordHash != Hash(oldPassword))
            {
                return false;
            }
            user.PasswordHash = Hash(newPassword);
            Audit(username, "Change Password", "Password updated");
            DataStore.SaveAll();
            return true;
        }

        public static bool HasRole(params string[] roles)
        {
            if (CurrentUser == null) return false;
            if (string.Equals(CurrentUser.Role, "Administrator", StringComparison.OrdinalIgnoreCase)) return true;
            foreach (var r in roles)
            {
                if (string.Equals(CurrentUser.Role, r, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        public static void Audit(string username, string action, string detail)
        {
            DataStore.AuditLogs.Add(new AuditLog
            {
                LogId = "L" + (DataStore.AuditLogs.Count + 1).ToString("D6"),
                Timestamp = DateTime.Now,
                Username = username ?? "(anonymous)",
                Action = action,
                Detail = detail
            });
        }
    }
}
