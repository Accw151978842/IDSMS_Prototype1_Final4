using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using Prototype1.Database;

namespace Prototype1.Forms
{
    public class MainMenuForm : Form
    {
        private StatusStrip         statusStrip;
        private ToolStripStatusLabel lblUserStatus, lblRoleStatus, lblDateStatus;
        private Panel               pnlBanner;
        private Panel               pnlBody;
        private Panel               pnlSidebar;
        private Panel               pnlSidebarScroll;   // scrollable inner panel
        private Panel               pnlScrollThumb;     // custom scrollbar thumb (replaces native)
        private Panel               pnlContent;
        private System.Windows.Forms.Timer dashTimer;
        private System.Windows.Forms.Timer sessionTimer;
        private Label   lblSession;
        private bool    warnShown;

        private static readonly Color SidebarBg    = Color.FromArgb(28,  42,  56);
        private static readonly Color SidebarGroup = Color.FromArgb(110, 130, 150);
        private static readonly Color NavBtnBack   = Color.FromArgb(28,  42,  56);
        private static readonly Color NavBtnHover  = Color.FromArgb(13,  94, 118);
        private static readonly Color NavBtnText   = Color.FromArgb(210, 220, 230);
        private static readonly Color NavBtnActive = Color.FromArgb(13,  94, 118);
        // Scrollbar thumb colors — invisible (matches sidebar) when idle, teal when hovered
        private static readonly Color ThumbIdle    = Color.FromArgb(28,  42,  56);   // matches SidebarBg
        private static readonly Color ThumbHover   = Color.FromArgb(13,  94, 118);   // matches NavBtnHover
        private static readonly Color ThumbDrag    = Color.FromArgb(19, 181, 166);   // teal accent

        // ── Custom scroll state (AutoScroll is OFF, we manage offset ourselves) ──
        private int  _scrollY = 0;           // current scroll offset (>=0)
        private int  _contentHeight = 0;     // total height of all nav items
        private bool _thumbDragging;
        private int  _thumbDragOffsetY;

        public MainMenuForm()
        {
            BuildUI();
        }

        // ============================================================
        //  BUILD SHELL
        // ============================================================
        private Button btnUserMenu;   // banner-right user dropdown trigger

        private void BuildUI()
        {
            Text          = "IDSMS — Premium Living Furniture Co. Ltd.";
            ClientSize    = new Size(1200, 720);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor     = UiTheme.Background;
            Font          = UiTheme.FontBase;
            MinimumSize   = new Size(1000, 640);

            // ---- BANNER ----
            pnlBanner = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = UiTheme.Primary };
            pnlBanner.Paint += (s, e) =>
            {
                using (var br = new System.Drawing.Drawing2D.LinearGradientBrush(
                    pnlBanner.ClientRectangle,
                    Color.FromArgb(10, 70, 90), Color.FromArgb(13, 94, 118),
                    System.Drawing.Drawing2D.LinearGradientMode.Horizontal))
                    e.Graphics.FillRectangle(br, pnlBanner.ClientRectangle);

                using (var fnt   = new Font(UiTheme.FontFamily, 14F, FontStyle.Bold))
                using (var brush = new SolidBrush(Color.White))
                    e.Graphics.DrawString("Premium Living Furniture Co. Ltd.", fnt, brush, new PointF(252, 10));

                using (var fnt   = new Font(UiTheme.FontFamily, 9F))
                using (var brush = new SolidBrush(Color.FromArgb(180, 210, 225)))
                    e.Graphics.DrawString("Integrated Delivery Services Management System (IDSMS)", fnt, brush, new PointF(254, 36));

                using (var pen = new Pen(UiTheme.Accent, 3))
                    e.Graphics.DrawLine(pen, 0, pnlBanner.Height - 1, pnlBanner.Width, pnlBanner.Height - 1);
            };
            Controls.Add(pnlBanner);

            // ---- BANNER user dropdown (top-right) ----
            btnUserMenu = new Button
            {
                Text       = "  Account  \u25BE",   // ▾
                Height     = 32,
                Width      = 200,
                FlatStyle  = FlatStyle.Flat,
                BackColor  = Color.FromArgb(10, 70, 90),
                ForeColor  = Color.White,
                Font       = new Font(UiTheme.FontFamily, 9.5F, FontStyle.Bold),
                TextAlign  = ContentAlignment.MiddleRight,
                Cursor     = Cursors.Hand,
                Padding    = new Padding(8, 0, 12, 0),
                Anchor     = AnchorStyles.Top | AnchorStyles.Right,
                UseVisualStyleBackColor = false
            };
            btnUserMenu.FlatAppearance.BorderSize         = 1;
            btnUserMenu.FlatAppearance.BorderColor        = Color.FromArgb(40, 110, 130);
            btnUserMenu.FlatAppearance.MouseOverBackColor = Color.FromArgb(20, 110, 135);
            btnUserMenu.FlatAppearance.MouseDownBackColor = Color.FromArgb(8,  60,  80);
            btnUserMenu.Click += (s, e) => ShowUserMenu();
            btnUserMenu.Location = new Point(pnlBanner.Width - btnUserMenu.Width - 16, 16);
            pnlBanner.Controls.Add(btnUserMenu);

            // ---- SESSION COUNTDOWN ----
            lblSession = new Label
            {
                AutoSize   = true,
                ForeColor  = Color.FromArgb(180, 220, 230),
                BackColor  = Color.Transparent,
                Font       = new Font(UiTheme.FontFamily, 9F),
                Text       = "Session 15:00"
            };
            pnlBanner.Controls.Add(lblSession);
            pnlBanner.Resize += (s, e) =>
            {
                btnUserMenu.Location = new Point(pnlBanner.Width - btnUserMenu.Width - 16, 16);
                lblSession.Location  = new Point(btnUserMenu.Location.X - lblSession.Width - 14,
                                                  16 + (btnUserMenu.Height - lblSession.Height) / 2);
            };
            // initial position
            lblSession.Location = new Point(btnUserMenu.Location.X - 130,
                                             16 + (btnUserMenu.Height - lblSession.Height) / 2);

            // ---- STATUS BAR ----
            statusStrip = new StatusStrip
            {
                BackColor  = UiTheme.Surface,
                Font       = new Font(UiTheme.FontFamily, 9F),
                SizingGrip = false
            };
            lblUserStatus = new ToolStripStatusLabel { Spring = false, ForeColor = UiTheme.TextPrimary };
            lblRoleStatus = new ToolStripStatusLabel { Spring = true,  TextAlign = ContentAlignment.MiddleCenter, ForeColor = UiTheme.TextMuted };
            lblDateStatus = new ToolStripStatusLabel { Spring = false, ForeColor = UiTheme.TextPrimary };
            statusStrip.Items.AddRange(new ToolStripItem[] { lblUserStatus, lblRoleStatus, lblDateStatus });
            Controls.Add(statusStrip);

            // ---- BODY ----
            pnlBody = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Background };
            Controls.Add(pnlBody);
            pnlBody.BringToFront();

            // ---- SIDEBAR ----
            pnlSidebar = new Panel { Dock = DockStyle.Left, Width = 220, BackColor = SidebarBg };

            // Logo row (declared first, but added LAST so DockStyle.Top sits above the Fill panel)
            // Match SidebarBg exactly to remove the visible dividing line
            var logoBlock = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = SidebarBg };
            var logoLabel = new Label
            {
                Text      = "  IDSMS",
                Font      = new Font(UiTheme.FontFamily, 14F, FontStyle.Bold),
                ForeColor = Color.White,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(10, 0, 0, 0)
            };
            logoBlock.Controls.Add(logoLabel);

            // Scrollable nav items — NO native scrollbar (AutoScroll = false)
            // Wheel + custom thumb scroll only. We track scroll offset manually.
            pnlSidebarScroll = new Panel
            {
                Dock       = DockStyle.Fill,
                BackColor  = SidebarBg,
                AutoScroll = false,                    // no native scrollbar at all
                Padding    = new Padding(0, 0, 8, 0)   // reserve 8px on right for custom thumb
            };
            pnlSidebarScroll.MouseWheel += SidebarScroll_MouseWheel;
            // Custom scrollbar thumb (overlays right edge of pnlSidebarScroll)
            pnlScrollThumb = new Panel
            {
                Width     = 6,
                BackColor = ThumbIdle,
                Cursor    = Cursors.Hand,
                Visible   = false   // hidden until content exceeds viewport
            };
            pnlScrollThumb.MouseDown += Thumb_MouseDown;
            pnlScrollThumb.MouseMove += Thumb_MouseMove;
            pnlScrollThumb.MouseUp   += Thumb_MouseUp;
            pnlScrollThumb.MouseEnter += (s, e) => { if (!_thumbDragging) pnlScrollThumb.BackColor = ThumbHover; };
            pnlScrollThumb.MouseLeave += (s, e) => { if (!_thumbDragging) pnlScrollThumb.BackColor = ThumbIdle;  };

            // Add Fill FIRST so the Top-docked logo correctly stacks above it
            pnlSidebar.Controls.Add(pnlSidebarScroll);
            pnlSidebar.Controls.Add(logoBlock);
            pnlSidebar.Controls.Add(pnlScrollThumb);   // overlay on top of scroll panel
            pnlScrollThumb.BringToFront();
            pnlBody.Controls.Add(pnlSidebar);

            // Hook resize to recalc thumb. Scrolling now driven entirely by our code.
            pnlSidebarScroll.Resize += (s, e) => UpdateThumb();

            // ---- CONTENT ----
            pnlContent = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Background, Padding = new Padding(28, 20, 28, 20) };
            pnlBody.Controls.Add(pnlContent);
            pnlContent.BringToFront();

            BuildSidebar();

            // Auto-refresh
            dashTimer = new System.Windows.Forms.Timer { Interval = 30000 };
            dashTimer.Tick += (s, e) => RefreshDashboard();
            dashTimer.Start();

            // ---- SESSION TIMER (ticks every second) ----
            SecurityService.TouchActivity();
            warnShown = false;
            sessionTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            sessionTimer.Tick += SessionTimer_Tick;
            sessionTimer.Start();

            // Reset idle timer on any user interaction across the form
            HookActivity(this);
            Shown       += (s, e) => RefreshDashboard();
            FormClosing += (s, e) => { dashTimer.Stop(); sessionTimer.Stop(); };
        }

        // ============================================================
        //  SIDEBAR
        // ============================================================
        private int _navY = 0;      // running Y position inside pnlSidebarScroll

        private void BuildSidebar()
        {
            pnlSidebarScroll.Controls.Clear();
            _navY = 8;

            // Each group: only emit header if at least one button is visible for current role.
            // Admin auto-passes via SecurityService.HasRole() bypass.

            AddGroupWithButtons("ORDER PROCESSING", new[] {
                Nav("  Sales Quotations",      () => new SalesQuotationForm(),    "Sales"),
                Nav("  Sales Orders",          () => new SalesOrderListForm(),    "Sales", "Logistics", "Warehouse"),
            });

            AddGroupWithButtons("LOGISTICS", new[] {
                Nav("  Delivery Notes",        () => new DeliveryNoteForm(),      "Logistics"),
                Nav("  Inward / Return Goods", () => new GoodsReceivedForm(),     "Logistics", "Warehouse"),
            });

            AddGroupWithButtons("INVENTORY", new[] {
                Nav("  Item Master / Stock",   () => new ItemMasterForm(),        "Warehouse", "Sales"),
            });

            AddGroupWithButtons("PRODUCTION", new[] {
                Nav("  Raw Material Requests", () => new RawMaterialRequestForm(),"Warehouse", "Logistics"),
            });

            AddGroupWithButtons("PROCUREMENT", new[] {
                Nav("  Purchase Orders",       () => new ProcurementForm(),       "Warehouse", "Logistics"),
            });

            AddGroupWithButtons("AFTER-SERVICE", new[] {
                Nav("  Return / Replacement",  () => new AfterServiceForm(),      "Service", "Sales"),
            });

            AddGroupWithButtons("MASTER DATA", new[] {
                Nav("  Customers",             () => new CustomerMasterForm(),    "Sales", "Service"),
                Nav("  Suppliers",             () => new SupplierMasterForm(),    "Warehouse", "Logistics"),
                Nav("  Staff",                 () => new StaffMasterForm(),       "Administrator"),
            });

            AddGroupWithButtons("ADMINISTRATION", new[] {
                Nav("  User Accounts",         () => new UserMasterForm(),        "Administrator"),
                Nav("  Audit Log",             () => new AuditLogForm(),          "Administrator"),
            });

            AddGroupWithButtons("REPORTS", new[] {
                Nav("  Statistical Reports",   () => new ReportForm(),            "Administrator", "Sales", "Logistics", "Warehouse"),
            });

            // Record content height for our custom scroll logic
            _contentHeight = _navY + 8;
            _scrollY = 0;
            ApplyScroll();
            UpdateThumb();
        }

        // ------------------------------------------------------------
        // RBAC-aware sidebar helpers
        // ------------------------------------------------------------
        private (string label, Func<Form> factory, string[] roles) Nav(
            string label, Func<Form> factory, params string[] roles)
            => (label, factory, roles);

        private void AddGroupWithButtons(string groupName,
            (string label, Func<Form> factory, string[] roles)[] items)
        {
            var visible = items.Where(i => SecurityService.HasRole(i.roles)).ToList();
            if (visible.Count == 0) return;   // hide entire group when no button is allowed
            AddNavGroup(groupName);
            foreach (var it in visible)
            {
                var factory = it.factory;
                AddNavButton(it.label, (s, e) => Open(factory()));
            }
        }

        // ============================================================
        //  CUSTOM SCROLL (no native scrollbar)
        // ============================================================
        private void SidebarScroll_MouseWheel(object sender, MouseEventArgs e)
        {
            int viewport = pnlSidebarScroll.ClientSize.Height;
            int maxScroll = Math.Max(0, _contentHeight - viewport);
            if (maxScroll <= 0) return;
            _scrollY -= e.Delta;           // typical wheel delta = ±120
            _scrollY = Math.Max(0, Math.Min(_scrollY, maxScroll));
            ApplyScroll();
            UpdateThumb();
        }

        private void ApplyScroll()
        {
            // Reposition every child of pnlSidebarScroll relative to _scrollY.
            // Each child's logical Y is stored in Tag (set by AddNavGroup / AddNavButton).
            pnlSidebarScroll.SuspendLayout();
            foreach (Control c in pnlSidebarScroll.Controls)
            {
                if (c.Tag is int logicalY)
                {
                    c.Top = logicalY - _scrollY;
                }
            }
            pnlSidebarScroll.ResumeLayout();
        }

        // ============================================================
        //  CUSTOM SCROLLBAR THUMB
        // ============================================================
        private void UpdateThumb()
        {
            if (pnlScrollThumb == null || pnlSidebarScroll == null) return;
            int viewport = pnlSidebarScroll.ClientSize.Height;
            int content  = _contentHeight;
            if (content <= viewport || content <= 0)
            {
                pnlScrollThumb.Visible = false;
                return;
            }
            pnlScrollThumb.Visible = true;

            int trackTop    = pnlSidebarScroll.Top;
            int trackHeight = pnlSidebarScroll.Height;
            int thumbHeight = Math.Max(30, (int)((double)viewport / content * trackHeight));

            int maxScroll = Math.Max(1, content - viewport);
            int thumbY = trackTop + (int)((double)_scrollY / maxScroll * (trackHeight - thumbHeight));

            pnlScrollThumb.SetBounds(
                pnlSidebar.Width - pnlScrollThumb.Width - 2,
                thumbY,
                pnlScrollThumb.Width,
                thumbHeight);
        }

        private void Thumb_MouseDown(object sender, MouseEventArgs e)
        {
            _thumbDragging   = true;
            _thumbDragOffsetY = e.Y;
            pnlScrollThumb.BackColor = ThumbDrag;
            pnlScrollThumb.Capture = true;
        }

        private void Thumb_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_thumbDragging) return;
            Point screenPt = pnlScrollThumb.PointToScreen(new Point(0, e.Y));
            Point trackPt  = pnlSidebar.PointToClient(screenPt);
            int trackTop    = pnlSidebarScroll.Top;
            int trackHeight = pnlSidebarScroll.Height;
            int thumbHeight = pnlScrollThumb.Height;
            int newThumbY = trackPt.Y - _thumbDragOffsetY;
            newThumbY = Math.Max(trackTop, Math.Min(newThumbY, trackTop + trackHeight - thumbHeight));

            int viewport = pnlSidebarScroll.ClientSize.Height;
            int content  = _contentHeight;
            int maxScroll = Math.Max(1, content - viewport);
            double pct = (double)(newThumbY - trackTop) / Math.Max(1, trackHeight - thumbHeight);
            _scrollY = (int)(pct * maxScroll);
            _scrollY = Math.Max(0, Math.Min(_scrollY, maxScroll));
            ApplyScroll();
            UpdateThumb();
        }

        private void Thumb_MouseUp(object sender, MouseEventArgs e)
        {
            _thumbDragging = false;
            pnlScrollThumb.Capture = false;
            pnlScrollThumb.BackColor = pnlScrollThumb.ClientRectangle.Contains(e.Location)
                                       ? ThumbHover
                                       : ThumbIdle;
        }

        // ============================================================
        //  BANNER USER DROPDOWN  (Change Password / Logout)
        // ============================================================
        private void ShowUserMenu()
        {
            var menu = new ContextMenuStrip
            {
                Font      = new Font(UiTheme.FontFamily, 9.5F),
                ShowImageMargin = false
            };

            if (SecurityService.CurrentUser != null)
            {
                var header = new ToolStripLabel(
                    SecurityService.CurrentUser.FullName +
                    "   (" + SecurityService.CurrentUser.Role + ")")
                {
                    Font      = new Font(UiTheme.FontFamily, 9F, FontStyle.Bold),
                    ForeColor = UiTheme.TextMuted,
                    Enabled   = false
                };
                menu.Items.Add(header);
                menu.Items.Add(new ToolStripSeparator());
            }

            var miChangePwd = new ToolStripMenuItem("Change Password...");
            miChangePwd.Click += (s, e) => Open(new ChangePasswordForm());
            menu.Items.Add(miChangePwd);

            menu.Items.Add(new ToolStripSeparator());

            var miAbout = new ToolStripMenuItem("About IDSMS");
            miAbout.Click += (s, e) => MessageBox.Show(this,
                "Integrated Delivery Services Management System\nPrototype I\n" +
                "Premium Living Furniture Co. Ltd.\nITP4915M Group 17",
                "About " + UiTheme.AppTitleLong, MessageBoxButtons.OK, MessageBoxIcon.Information);
            menu.Items.Add(miAbout);

            menu.Items.Add(new ToolStripSeparator());

            var miLogout = new ToolStripMenuItem("Logout");
            miLogout.ForeColor = Color.FromArgb(192, 57, 43);
            miLogout.Click += (s, e) => DoLogout();
            menu.Items.Add(miLogout);

            // Drop the menu under the button, right-aligned to it
            var p = btnUserMenu.PointToScreen(new Point(0, btnUserMenu.Height));
            menu.Show(p);
        }

        private void AddNavGroup(string text)
        {
            var lbl = new Label
            {
                Text      = text,
                Font      = new Font(UiTheme.FontFamily, 7.5F, FontStyle.Bold),
                ForeColor = SidebarGroup,
                AutoSize  = false,
                Width     = pnlSidebar.Width - 16,
                Height    = 26,
                TextAlign = ContentAlignment.BottomLeft,
                Location  = new Point(8, _navY),
                Padding   = new Padding(6, 0, 0, 0),
                Tag       = _navY            // remember logical Y for custom scroll
            };
            pnlSidebarScroll.Controls.Add(lbl);
            _navY += 30;
        }

        private void AddNavButton(string text, EventHandler onClick)
        {
            int logicalY = _navY;
            var btn = new Button
            {
                Text      = text,
                Width     = pnlSidebar.Width - 16,
                Height    = 34,
                Location  = new Point(8, _navY),
                TextAlign = ContentAlignment.MiddleLeft,
                FlatStyle = FlatStyle.Flat,
                BackColor = NavBtnBack,
                ForeColor = NavBtnText,
                Font      = UiTheme.FontNormal,
                Cursor    = Cursors.Hand,
                Padding   = new Padding(10, 0, 4, 0),
                UseVisualStyleBackColor = false,
                Tag       = logicalY            // remember logical Y for custom scroll
            };
            btn.FlatAppearance.BorderSize         = 0;
            btn.FlatAppearance.MouseOverBackColor = NavBtnHover;
            btn.FlatAppearance.MouseDownBackColor = NavBtnActive;
            btn.Click += onClick;
            // Forward mouse wheel over the button to the sidebar scroll panel
            btn.MouseWheel += SidebarScroll_MouseWheel;
            pnlSidebarScroll.Controls.Add(btn);
            _navY += 36;
        }

        // ============================================================
        //  DASHBOARD
        // ============================================================
        private void RefreshDashboard()
        {
            var en = CultureInfo.CreateSpecificCulture("en-US");
            if (SecurityService.CurrentUser != null)
            {
                lblUserStatus.Text = "  " + SecurityService.CurrentUser.FullName +
                                     " (" + SecurityService.CurrentUser.Username + ")";
                lblRoleStatus.Text = SecurityService.CurrentUser.Role;

                // Banner dropdown trigger shows the username
                if (btnUserMenu != null)
                    btnUserMenu.Text = "  " + SecurityService.CurrentUser.FullName + "  \u25BE";
            }
            lblDateStatus.Text = DateTime.Now.ToString("yyyy-MM-dd  HH:mm", en) + "  ";

            DataStore.LoadAll();
            RenderDashboard();
        }

        private void RenderDashboard()
        {
            pnlContent.Controls.Clear();
            var en = CultureInfo.CreateSpecificCulture("en-US");

            // ---- Welcome ----
            string name = SecurityService.CurrentUser != null ? SecurityService.CurrentUser.FullName : "";
            var lblWelcome = new Label
            {
                Text      = "Good " + Greeting() + ", " + name + " ",
                Font      = new Font(UiTheme.FontFamily, 16F, FontStyle.Bold),
                ForeColor = UiTheme.TextPrimary,
                AutoSize  = true,
                Location  = new Point(0, 0)
            };
            var lblDate = new Label
            {
                Text      = DateTime.Now.ToString("dddd, dd MMMM yyyy", en),
                Font      = new Font(UiTheme.FontFamily, 9.5F),
                ForeColor = UiTheme.TextMuted,
                AutoSize  = true,
                Location  = new Point(0, 34)
            };
            pnlContent.Controls.Add(lblWelcome);
            pnlContent.Controls.Add(lblDate);

            // ---- Divider ----
            var div = new Panel
            {
                Location  = new Point(0, 62),
                Height    = 1,
                BackColor = UiTheme.BorderLight,
                Anchor    = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            pnlContent.Controls.Add(div);
            void SyncDivWidth(object s, EventArgs e2) => div.Width = pnlContent.ClientSize.Width - pnlContent.Padding.Horizontal;
            pnlContent.Resize += SyncDivWidth;
            SyncDivWidth(null, null);

            // ---- Section label ----
            pnlContent.Controls.Add(new Label
            {
                Text      = "OVERVIEW",
                Font      = new Font(UiTheme.FontFamily, 8F, FontStyle.Bold),
                ForeColor = UiTheme.TextMuted,
                AutoSize  = true,
                Location  = new Point(0, 74)
            });

            // ---- Tile panel  (fixed row height to keep tiles equal) ----
            var tilePanel = new TableLayoutPanel
            {
                Location    = new Point(0, 96),
                ColumnCount = 3,
                RowCount    = 2,
                Anchor      = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tilePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));
            tilePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            tilePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            // Fixed row heights so all tiles are the same size
            tilePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 120F));
            tilePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 120F));
            tilePanel.Height = 240;

            int openOrders     = CountOpenOrders();
            int pendingDel     = CountPendingDeliveries();
            int openService    = CountOpenService();
            int lowStock       = CountLowStock();
            int totalCustomers = DataStore.Customers.Count;
            int totalItems     = DataStore.Items.Count;

            // Role-filtered dashboard tiles. Auto-grid so layout adapts when some tiles are hidden.
            var tiles = new List<(string title, string value, string sub, Color color, Func<Form> factory, string[] roles)>
            {
                ("Open Orders",        openOrders.ToString(),     "Click to view",
                    Color.FromArgb(13, 94, 118),  () => new SalesOrderListForm(), new[]{"Sales","Logistics","Warehouse"}),
                ("Pending Deliveries", pendingDel.ToString(),     "Click to view",
                    Color.FromArgb(39, 174, 96),  () => new DeliveryNoteForm(),   new[]{"Logistics"}),
                ("After-Service Open", openService.ToString(),    "Click to view",
                    Color.FromArgb(230, 126, 34), () => new AfterServiceForm(),   new[]{"Service","Sales"}),
                ("Low Stock Items",    lowStock.ToString(),
                    lowStock > 0 ? "Needs attention" : "All OK",
                    lowStock > 0 ? Color.FromArgb(192, 57, 43) : Color.FromArgb(39, 174, 96),
                    () => new ItemMasterForm(),   new[]{"Warehouse","Sales"}),
                ("Total Customers",    totalCustomers.ToString(), "Click to view",
                    Color.FromArgb(125, 60, 152), () => new CustomerMasterForm(), new[]{"Sales","Service"}),
                ("Total Items",        totalItems.ToString(),     "Click to view",
                    Color.FromArgb(52, 73, 94),   () => new ItemMasterForm(),     new[]{"Warehouse","Sales"}),
            };

            var visibleTiles = tiles.Where(t => SecurityService.HasRole(t.roles)).ToList();
            int cols = 3;
            int rows = Math.Max(1, (int)Math.Ceiling(visibleTiles.Count / (double)cols));
            // resize panel rows to match
            tilePanel.RowStyles.Clear();
            tilePanel.RowCount = rows;
            for (int r = 0; r < rows; r++)
                tilePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 120F));
            tilePanel.Height = rows * 120;

            for (int i = 0; i < visibleTiles.Count; i++)
            {
                var t = visibleTiles[i];
                var factory = t.factory;
                tilePanel.Controls.Add(
                    MakeTile(t.title, t.value, t.sub, t.color,
                        (s, e) => Open(factory())),
                    i % cols, i / cols);
            }

            pnlContent.Controls.Add(tilePanel);
            void SyncTileWidth(object s, EventArgs e2) => tilePanel.Width = pnlContent.ClientSize.Width - pnlContent.Padding.Horizontal;
            pnlContent.Resize += SyncTileWidth;
            SyncTileWidth(null, null);

            // ---- Recent audit ----
            pnlContent.Controls.Add(new Label
            {
                Text      = "RECENT AUDIT ACTIVITY",
                Font      = new Font(UiTheme.FontFamily, 8F, FontStyle.Bold),
                ForeColor = UiTheme.TextMuted,
                AutoSize  = true,
                Location  = new Point(0, 358)
            });

            var recentGrid = new DataGridView
            {
                Location              = new Point(0, 378),
                Height                = 180,
                ReadOnly              = true,
                AllowUserToAddRows    = false,
                AllowUserToDeleteRows = false,
                SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect           = false,
                AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill,
                Anchor                = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            UiTheme.ApplyGrid(recentGrid);
            recentGrid.Columns.Add("Time",   "Time");
            recentGrid.Columns.Add("User",   "User");
            recentGrid.Columns.Add("Action", "Action");
            recentGrid.Columns.Add("Detail", "Detail");
            recentGrid.Columns["Time"].Width = 150;
            recentGrid.Columns["User"].Width = 110;

            int shown = 0;
            foreach (var log in DataStore.AuditLogs)
            {
                if (shown >= 6) break;
                recentGrid.Rows.Add(log.Timestamp.ToString("yyyy-MM-dd HH:mm", en), log.Username, log.Action, log.Detail);
                shown++;
            }
            pnlContent.Controls.Add(recentGrid);
            void SyncGridWidth(object s, EventArgs e2) => recentGrid.Width = pnlContent.ClientSize.Width - pnlContent.Padding.Horizontal;
            pnlContent.Resize += SyncGridWidth;
            SyncGridWidth(null, null);
        }

        // ============================================================
        //  TILE BUILDER
        // ============================================================
        private Panel MakeTile(string title, string value, string hint, Color accent, EventHandler onClick)
        {
            var card = UiTheme.BuildCard();
            card.Dock   = DockStyle.Fill;
            card.Margin = new Padding(5);
            card.Cursor = Cursors.Hand;

            // Left accent bar
            var bar = new Panel { Dock = DockStyle.Left, Width = 5, BackColor = accent };
            card.Controls.Add(bar);

            // Hint at bottom
            var lblHint = new Label
            {
                Text      = hint,
                Font      = new Font(UiTheme.FontFamily, 7.5F),
                ForeColor = UiTheme.TextMuted,
                Dock      = DockStyle.Bottom,
                Height    = 18,
                TextAlign = ContentAlignment.MiddleCenter
            };
            card.Controls.Add(lblHint);

            // Title above hint
            var lblTitle = new Label
            {
                Text      = title,
                Font      = new Font(UiTheme.FontFamily, 9.5F, FontStyle.Bold),
                ForeColor = UiTheme.TextPrimary,
                Dock      = DockStyle.Bottom,
                Height    = 22,
                TextAlign = ContentAlignment.MiddleCenter
            };
            card.Controls.Add(lblTitle);

            // Value number (Fill remaining space)
            var lblVal = new Label
            {
                Text      = value,
                Font      = new Font(UiTheme.FontFamily, 28F, FontStyle.Bold),
                ForeColor = accent,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            card.Controls.Add(lblVal);

            // Hover + click wiring
            Action hover = () => card.BackColor = UiTheme.PrimaryLight;
            Action leave = () => card.BackColor = UiTheme.Surface;

            foreach (Control c in new Control[] { card, lblVal, lblTitle, lblHint })
            {
                c.Click      += onClick;
                c.Cursor      = Cursors.Hand;
                c.MouseEnter += (s, e) => hover();
                c.MouseLeave += (s, e) => leave();
            }

            return card;
        }

        // ============================================================
        //  HELPERS
        // ============================================================
        private string Greeting()
        {
            int h = DateTime.Now.Hour;
            return h < 12 ? "Morning" : h < 18 ? "Afternoon" : "Evening";
        }

        private int CountOpenOrders()       { int n = 0; foreach (var o in DataStore.SalesOrders)    if (o.Status == "Pending" || o.Status == "Confirmed")   n++; return n; }
        private int CountPendingDeliveries(){ int n = 0; foreach (var d in DataStore.Deliveries)     if (d.Status == "Scheduled" || d.Status == "In Transit") n++; return n; }
        private int CountOpenService()      { int n = 0; foreach (var r in DataStore.ServiceRequests) if (r.Status == "Open" || r.Status == "Processing")     n++; return n; }
        private int CountLowStock()         { int n = 0; foreach (var i in DataStore.Items)           if (i.StockQty <= i.ReorderLevel)                       n++; return n; }

        // ============================================================
        //  SESSION TIMEOUT
        // ============================================================

        private void SessionTimer_Tick(object sender, EventArgs e)
        {
            if (SecurityService.CurrentUser == null) return;

            int secLeft = SecurityService.SecondsLeft();
            int mm = Math.Max(0, secLeft) / 60;
            int ss = Math.Max(0, secLeft) % 60;

            // Recolor when running low
            if (secLeft <= SecurityService.SessionWarnBeforeMinutes * 60)
                lblSession.ForeColor = Color.FromArgb(255, 180, 100); // amber
            else
                lblSession.ForeColor = Color.FromArgb(180, 220, 230);

            lblSession.Text = string.Format("Session {0:D2}:{1:D2}", mm, ss);
            // re-anchor after width change
            lblSession.Location = new Point(btnUserMenu.Location.X - lblSession.Width - 14,
                                             16 + (btnUserMenu.Height - lblSession.Height) / 2);

            // Warn 2 minutes before expiry
            if (!warnShown && secLeft <= SecurityService.SessionWarnBeforeMinutes * 60 && secLeft > 0)
            {
                warnShown = true;
                var result = MessageBox.Show(this,
                    "You will be logged out in " + SecurityService.SessionWarnBeforeMinutes +
                    " minutes due to inactivity.\n\nClick OK to stay signed in.",
                    UiTheme.AppTitle + " - Session Expiring",
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
                if (result == DialogResult.OK)
                {
                    SecurityService.TouchActivity();
                    warnShown = false;
                }
                // If user clicked Cancel or closed dialog, leave warnShown=true
                // so we don't re-prompt; timer will still fire auto-logout below.
            }

            // Auto-logout
            if (secLeft <= 0)
            {
                sessionTimer.Stop();
                MessageBox.Show(this,
                    "Your session has expired due to inactivity. Please sign in again.",
                    UiTheme.AppTitle + " - Session Expired",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                DoLogout();
            }
        }

        /// <summary>Attach activity listeners recursively to every control.</summary>
        private void HookActivity(Control root)
        {
            root.MouseMove += Activity_Handler;
            root.MouseClick += Activity_Handler;
            root.KeyDown   += Activity_Handler;
            foreach (Control c in root.Controls)
            {
                HookActivity(c);
            }
            root.ControlAdded += (s, e) => HookActivity(e.Control);
        }

        private void Activity_Handler(object sender, EventArgs e)
        {
            if (SecurityService.CurrentUser != null)
            {
                SecurityService.TouchActivity();
                warnShown = false;
            }
        }

        private void DoLogout()
        {
            // Clear the current session ONLY.
            // DO NOT clear DataStore lists - they hold the in-memory user table,
            // staff, customers, etc. Wiping them would break subsequent logins
            // (FirstOrDefault on an empty Users list returns null -> "Invalid username").
            SecurityService.Logout();
            DialogResult = DialogResult.Retry;
            Close();
        }

        private void Open(Form f)
        {
            f.StartPosition = FormStartPosition.CenterParent;
            f.ShowDialog(this);
            RefreshDashboard();
        }

        private void RequireRoleAndOpen(Form f, params string[] roles)
        {
            if (!SecurityService.HasRole(roles))
            {
                MessageBox.Show(this, "Your role does not have permission to use this function.",
                    UiTheme.AppTitle + " - Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                f.Dispose();
                return;
            }
            Open(f);
        }
    }
}
