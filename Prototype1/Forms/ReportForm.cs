using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Prototype1.Models;
using Prototype1.Database;

namespace Prototype1.Forms
{
    /// <summary>
    /// Statistical Reports module (Advanced Stage requirement).
    ///
    /// Provides management-level statistics across the business with KPI
    /// summary cards, a self-drawn bar chart (no external charting library,
    /// so the project keeps building with the standard WinForms references)
    /// and a detail data grid. Each report can be printed / previewed as a
    /// plain-text statement, consistent with the rest of the prototype.
    /// </summary>
    public class ReportForm : Form
    {
        // A single category + value pair for the bar chart.
        private class ChartItem
        {
            public string Label;
            public decimal Value;
            public Color Color;
            public string ValueText;       // formatted value shown on the bar
        }

        private ComboBox cmbReport;
        private DateTimePicker dtpFrom, dtpTo;
        private CheckBox chkDateRange;
        private TableLayoutPanel pnlKpis;
        private ChartPanel chart;
        private DataGridView grid;
        private Button btnRefresh, btnPrint, btnClose;
        private Label lblChartTitle;

        private string _lastReportText = "";

        public ReportForm()
        {
            Text = "Statistical Reports";
            ClientSize = new Size(1000, 680);
            StartPosition = FormStartPosition.CenterParent;
            UiTheme.ApplyForm(this);
            BuildUI();
            RunReport();
        }

        private void BuildUI()
        {
            // ---- TOP toolbar (filters) ----
            var top = UiTheme.BuildToolbar(110);

            var lblTitle = UiTheme.BuildHeading("Statistical Reports");
            lblTitle.Location = new Point(0, 8);
            top.Controls.Add(lblTitle);
            top.Controls.Add(new Label
            {
                Text = "Management statistics across sales, inventory and logistics.",
                Location = new Point(2, 40), AutoSize = true, ForeColor = UiTheme.TextMuted
            });

            top.Controls.Add(new Label { Text = "Report:", Location = new Point(0, 78), AutoSize = true, ForeColor = UiTheme.TextMuted });
            cmbReport = new ComboBox { Location = new Point(56, 75), Width = 280, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbReport.Items.AddRange(new object[]
            {
                "Sales by Status",
                "Monthly Sales Trend (Amount)",
                "Top 5 Selling Items (Qty)",
                "Top 5 Customers (Amount)",
                "Stock Level by Item",
                "Low Stock Items",
                "Delivery Status Breakdown"
            });
            cmbReport.SelectedIndex = 0;
            cmbReport.SelectedIndexChanged += (s, e) => RunReport();
            top.Controls.Add(cmbReport);

            chkDateRange = new CheckBox { Text = "Filter by order date", Location = new Point(356, 77), AutoSize = true, ForeColor = UiTheme.TextPrimary };
            chkDateRange.CheckedChanged += (s, e) => { dtpFrom.Enabled = dtpTo.Enabled = chkDateRange.Checked; RunReport(); };
            top.Controls.Add(chkDateRange);

            top.Controls.Add(new Label { Text = "From:", Location = new Point(500, 78), AutoSize = true, ForeColor = UiTheme.TextMuted });
            dtpFrom = new DateTimePicker { Location = new Point(540, 75), Width = 120, Format = DateTimePickerFormat.Short, Enabled = false, Value = DateTime.Today.AddMonths(-6) };
            top.Controls.Add(dtpFrom);

            top.Controls.Add(new Label { Text = "To:", Location = new Point(672, 78), AutoSize = true, ForeColor = UiTheme.TextMuted });
            dtpTo = new DateTimePicker { Location = new Point(700, 75), Width = 120, Format = DateTimePickerFormat.Short, Enabled = false, Value = DateTime.Today };
            top.Controls.Add(dtpTo);

            // Keep the range valid: From can never be later than To, and To never earlier than From.
            dtpFrom.ValueChanged += (s, e) =>
            {
                if (dtpTo.MinDate != dtpFrom.Value.Date) dtpTo.MinDate = dtpFrom.Value.Date;
                if (chkDateRange.Checked) RunReport();
            };
            dtpTo.ValueChanged += (s, e) =>
            {
                if (dtpFrom.MaxDate != dtpTo.Value.Date) dtpFrom.MaxDate = dtpTo.Value.Date;
                if (chkDateRange.Checked) RunReport();
            };
            // Apply the initial constraint based on the default values.
            dtpTo.MinDate = dtpFrom.Value.Date;
            dtpFrom.MaxDate = dtpTo.Value.Date;

            // ---- BOTTOM bar ----
            var bottom = UiTheme.BuildBottomBar(64);
            btnRefresh = new Button { Text = "Refresh", Width = 110, Height = 34 };
            btnRefresh.Click += (s, e) => RunReport();
            btnPrint = new Button { Text = "Print / Preview", Width = 140, Height = 34 };
            btnPrint.Click += (s, e) => PreviewReport();
            btnClose = new Button { Text = "Close", Width = 100, Height = 34, DialogResult = DialogResult.Cancel };
            bottom.Controls.AddRange(new Control[] { btnRefresh, btnPrint, btnClose });
            UiTheme.StylePrimary(btnRefresh);
            UiTheme.StyleSecondary(btnPrint);
            UiTheme.StyleSecondary(btnClose);
            UiTheme.LayoutLeft(bottom, 10, btnRefresh, btnPrint, btnClose);
            CancelButton = btnClose;

            // ---- FILL (body) ----
            var body = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Background, Padding = new Padding(16, 8, 16, 8) };

            // KPI strip across the top of the body - TableLayoutPanel so cards
            // stretch and stay evenly distributed when the window is resized/maximized.
            pnlKpis = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 92,
                BackColor = UiTheme.Background,
                Padding = new Padding(0, 0, 0, 8),
                ColumnCount = 1,
                RowCount = 1
            };

            // Chart card (middle)
            var chartCard = new Panel { Dock = DockStyle.Top, Height = 280, BackColor = UiTheme.Surface, Padding = new Padding(12), Margin = new Padding(0) };
            lblChartTitle = new Label
            {
                Dock = DockStyle.Top,
                Height = 26,
                Text = "Chart",
                Font = new Font(UiTheme.FontFamily, 10.5F, FontStyle.Bold),
                ForeColor = UiTheme.TextPrimary
            };
            chart = new ChartPanel { Dock = DockStyle.Fill, BackColor = UiTheme.Surface };
            chartCard.Controls.Add(chart);
            chartCard.Controls.Add(lblChartTitle);

            // Data grid (bottom of body)
            grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            UiTheme.ApplyGrid(grid);
            var gridCard = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Surface, Padding = new Padding(1) };
            gridCard.Controls.Add(grid);

            // Add in reverse dock order so Fill grid sits below the chart.
            body.Controls.Add(gridCard);     // Fill
            body.Controls.Add(chartCard);    // Top
            body.Controls.Add(pnlKpis);      // Top (added last => topmost)

            Controls.Add(body);
            Controls.Add(UiTheme.BuildSeparator(DockStyle.Top));
            Controls.Add(top);
            Controls.Add(UiTheme.BuildSeparator(DockStyle.Bottom));
            Controls.Add(bottom);
        }

        // -------------------------------------------------------------
        //  Report dispatch
        // -------------------------------------------------------------
        private void RunReport()
        {
            if (cmbReport == null) return;
            string report = cmbReport.SelectedItem as string ?? "Sales by Status";

            switch (report)
            {
                case "Sales by Status":              ReportSalesByStatus();   break;
                case "Monthly Sales Trend (Amount)": ReportMonthlyTrend();    break;
                case "Top 5 Selling Items (Qty)":    ReportTopItems();        break;
                case "Top 5 Customers (Amount)":     ReportTopCustomers();    break;
                case "Stock Level by Item":          ReportStockLevels();     break;
                case "Low Stock Items":              ReportLowStock();        break;
                case "Delivery Status Breakdown":    ReportDeliveryStatus();  break;
            }
        }

        // Orders filtered by the optional order-date range.
        private List<SalesOrder> FilteredOrders()
        {
            IEnumerable<SalesOrder> q = DataStore.SalesOrders;
            if (chkDateRange != null && chkDateRange.Checked)
            {
                DateTime from = dtpFrom.Value.Date;
                DateTime to = dtpTo.Value.Date;
                q = q.Where(o => o.OrderDate.Date >= from && o.OrderDate.Date <= to);
            }
            return q.ToList();
        }

        // -------------------------------------------------------------
        //  Individual reports
        // -------------------------------------------------------------
        private void ReportSalesByStatus()
        {
            var orders = FilteredOrders();
            var statuses = new[] { "Pending", "Confirmed", "Shipped", "Completed", "Cancelled" };
            var colors = new[] { UiTheme.Warning, UiTheme.Primary, UiTheme.Accent, UiTheme.Success, UiTheme.Danger };

            var items = new List<ChartItem>();
            var rows = new List<string[]>();
            decimal grandTotal = 0m;
            int grandCount = 0;
            for (int i = 0; i < statuses.Length; i++)
            {
                var matching = orders.Where(o => string.Equals(o.Status, statuses[i], StringComparison.OrdinalIgnoreCase)).ToList();
                decimal amt = matching.Sum(o => o.TotalAmount);
                items.Add(new ChartItem { Label = statuses[i], Value = matching.Count, Color = colors[i], ValueText = matching.Count.ToString() });
                rows.Add(new[] { statuses[i], matching.Count.ToString(), "HKD " + amt.ToString("N2") });
                grandTotal += amt;
                grandCount += matching.Count;
            }

            SetKpis(
                Kpi("Total Orders", grandCount.ToString(), UiTheme.Primary),
                Kpi("Total Value", "HKD " + grandTotal.ToString("N0"), UiTheme.Accent),
                Kpi("Completed", orders.Count(o => o.Status == "Completed").ToString(), UiTheme.Success),
                Kpi("Cancelled", orders.Count(o => o.Status == "Cancelled").ToString(), UiTheme.Danger));

            lblChartTitle.Text = "Order Count by Status";
            chart.SetData(items);
            FillGrid(new[] { "Status", "Order Count", "Total Value" }, rows);
            BuildReportText("Sales by Status", new[] { "Status", "Order Count", "Total Value" }, rows,
                "Total orders: " + grandCount + "   Total value: HKD " + grandTotal.ToString("N2"));
        }

        private void ReportMonthlyTrend()
        {
            var orders = FilteredOrders().Where(o => o.Status != "Cancelled").ToList();
            // Group by YYYY-MM for the most recent 12 months that have data.
            var grouped = orders
                .GroupBy(o => o.OrderDate.ToString("yyyy-MM"))
                .OrderBy(g => g.Key)
                .ToList();

            var items = new List<ChartItem>();
            var rows = new List<string[]>();
            decimal total = 0m;
            foreach (var g in grouped)
            {
                decimal amt = g.Sum(o => o.TotalAmount);
                total += amt;
                items.Add(new ChartItem { Label = g.Key, Value = amt, Color = UiTheme.Primary, ValueText = amt.ToString("N0") });
                rows.Add(new[] { g.Key, g.Count().ToString(), "HKD " + amt.ToString("N2") });
            }

            decimal avg = grouped.Count > 0 ? total / grouped.Count : 0m;
            SetKpis(
                Kpi("Months", grouped.Count.ToString(), UiTheme.Primary),
                Kpi("Total Revenue", "HKD " + total.ToString("N0"), UiTheme.Accent),
                Kpi("Avg / Month", "HKD " + avg.ToString("N0"), UiTheme.Success));

            lblChartTitle.Text = "Monthly Sales Amount (excludes Cancelled)";
            chart.SetData(items);
            FillGrid(new[] { "Month", "Orders", "Revenue" }, rows);
            BuildReportText("Monthly Sales Trend", new[] { "Month", "Orders", "Revenue" }, rows,
                "Total revenue: HKD " + total.ToString("N2") + "   Avg/month: HKD " + avg.ToString("N2"));
        }

        private void ReportTopItems()
        {
            var orders = FilteredOrders().Where(o => o.Status != "Cancelled");
            var tally = new Dictionary<string, int>();
            var names = new Dictionary<string, string>();
            foreach (var o in orders)
                foreach (var ln in o.Lines)
                {
                    if (string.IsNullOrEmpty(ln.ItemId)) continue;
                    if (tally.ContainsKey(ln.ItemId)) tally[ln.ItemId] += ln.Quantity;
                    else { tally[ln.ItemId] = ln.Quantity; names[ln.ItemId] = ln.ItemName; }
                }

            var top = tally.OrderByDescending(kv => kv.Value).Take(5).ToList();
            var items = new List<ChartItem>();
            var rows = new List<string[]>();
            foreach (var kv in top)
            {
                string nm = names.ContainsKey(kv.Key) ? names[kv.Key] : kv.Key;
                items.Add(new ChartItem { Label = nm, Value = kv.Value, Color = UiTheme.Accent, ValueText = kv.Value.ToString() });
                rows.Add(new[] { kv.Key, nm, kv.Value.ToString() });
            }

            SetKpis(
                Kpi("Distinct Items Sold", tally.Count.ToString(), UiTheme.Primary),
                Kpi("Total Units Sold", tally.Values.Sum().ToString(), UiTheme.Accent));

            lblChartTitle.Text = "Top 5 Selling Items by Quantity";
            chart.SetData(items);
            FillGrid(new[] { "Item ID", "Item Name", "Units Sold" }, rows);
            BuildReportText("Top 5 Selling Items", new[] { "Item ID", "Item Name", "Units Sold" }, rows,
                "Distinct items sold: " + tally.Count + "   Total units: " + tally.Values.Sum());
        }

        private void ReportTopCustomers()
        {
            var orders = FilteredOrders().Where(o => o.Status != "Cancelled");
            var tally = new Dictionary<string, decimal>();
            foreach (var o in orders)
            {
                if (string.IsNullOrEmpty(o.CustomerId)) continue;
                if (tally.ContainsKey(o.CustomerId)) tally[o.CustomerId] += o.TotalAmount;
                else tally[o.CustomerId] = o.TotalAmount;
            }

            var top = tally.OrderByDescending(kv => kv.Value).Take(5).ToList();
            var items = new List<ChartItem>();
            var rows = new List<string[]>();
            foreach (var kv in top)
            {
                var c = DataStore.Customers.FirstOrDefault(x => x.CustomerId == kv.Key);
                string nm = c != null ? c.CompanyName : kv.Key;
                items.Add(new ChartItem { Label = nm, Value = kv.Value, Color = UiTheme.Primary, ValueText = kv.Value.ToString("N0") });
                rows.Add(new[] { kv.Key, nm, "HKD " + kv.Value.ToString("N2") });
            }

            SetKpis(
                Kpi("Active Customers", tally.Count.ToString(), UiTheme.Primary),
                Kpi("Total Value", "HKD " + tally.Values.Sum().ToString("N0"), UiTheme.Accent));

            lblChartTitle.Text = "Top 5 Customers by Order Value";
            chart.SetData(items);
            FillGrid(new[] { "Customer ID", "Company", "Total Value" }, rows);
            BuildReportText("Top 5 Customers", new[] { "Customer ID", "Company", "Total Value" }, rows,
                "Active customers: " + tally.Count + "   Total value: HKD " + tally.Values.Sum().ToString("N2"));
        }

        private void ReportStockLevels()
        {
            var sorted = DataStore.Items.OrderBy(i => i.StockQty).Take(12).ToList();
            var items = new List<ChartItem>();
            var rows = new List<string[]>();
            foreach (var it in sorted)
            {
                Color c = it.StockQty <= it.ReorderLevel ? UiTheme.Danger : UiTheme.Accent;
                items.Add(new ChartItem { Label = it.ItemName, Value = it.StockQty, Color = c, ValueText = it.StockQty.ToString() });
                rows.Add(new[] { it.ItemId, it.ItemName, it.StockQty.ToString(), it.ReorderLevel.ToString(),
                    it.StockQty <= it.ReorderLevel ? "LOW" : "OK" });
            }

            int totalUnits = DataStore.Items.Sum(i => i.StockQty);
            int lowCount = DataStore.Items.Count(i => i.StockQty <= i.ReorderLevel);
            SetKpis(
                Kpi("Total Items", DataStore.Items.Count.ToString(), UiTheme.Primary),
                Kpi("Total Stock Units", totalUnits.ToString("N0"), UiTheme.Accent),
                Kpi("Low Stock", lowCount.ToString(), UiTheme.Danger));

            lblChartTitle.Text = "Stock Level (lowest 12 items; red = at/below reorder)";
            chart.SetData(items);
            FillGrid(new[] { "Item ID", "Item Name", "Stock Qty", "Reorder Level", "Flag" }, rows);
            BuildReportText("Stock Level by Item", new[] { "Item ID", "Item Name", "Stock Qty", "Reorder Level", "Flag" }, rows,
                "Total items: " + DataStore.Items.Count + "   Total units: " + totalUnits + "   Low stock: " + lowCount);
        }

        private void ReportLowStock()
        {
            var low = DataStore.Items.Where(i => i.StockQty <= i.ReorderLevel).OrderBy(i => i.StockQty).ToList();
            var items = new List<ChartItem>();
            var rows = new List<string[]>();
            foreach (var it in low)
            {
                items.Add(new ChartItem { Label = it.ItemName, Value = it.StockQty, Color = UiTheme.Danger, ValueText = it.StockQty.ToString() });
                rows.Add(new[] { it.ItemId, it.ItemName, it.StockQty.ToString(), it.ReorderLevel.ToString(),
                    (it.ReorderLevel - it.StockQty).ToString() });
            }

            SetKpis(
                Kpi("Low Stock Items", low.Count.ToString(), UiTheme.Danger),
                Kpi("Total Items", DataStore.Items.Count.ToString(), UiTheme.Primary));

            lblChartTitle.Text = "Low Stock Items (at or below reorder level)";
            chart.SetData(items);
            FillGrid(new[] { "Item ID", "Item Name", "Stock Qty", "Reorder Level", "Shortfall" }, rows);
            BuildReportText("Low Stock Items", new[] { "Item ID", "Item Name", "Stock Qty", "Reorder Level", "Shortfall" }, rows,
                "Items at or below reorder level: " + low.Count);
        }

        private void ReportDeliveryStatus()
        {
            var statuses = new[] { "Scheduled", "In Transit", "Delivered", "Failed" };
            var colors = new[] { UiTheme.Warning, UiTheme.Primary, UiTheme.Success, UiTheme.Danger };
            var items = new List<ChartItem>();
            var rows = new List<string[]>();
            for (int i = 0; i < statuses.Length; i++)
            {
                int cnt = DataStore.Deliveries.Count(d => string.Equals(d.Status, statuses[i], StringComparison.OrdinalIgnoreCase));
                items.Add(new ChartItem { Label = statuses[i], Value = cnt, Color = colors[i], ValueText = cnt.ToString() });
                rows.Add(new[] { statuses[i], cnt.ToString() });
            }

            int acked = DataStore.Deliveries.Count(d => d.ReplySlipStatus == "Acknowledged");
            SetKpis(
                Kpi("Total Deliveries", DataStore.Deliveries.Count.ToString(), UiTheme.Primary),
                Kpi("Delivered", DataStore.Deliveries.Count(d => d.Status == "Delivered").ToString(), UiTheme.Success),
                Kpi("Reply Acknowledged", acked.ToString(), UiTheme.Accent));

            lblChartTitle.Text = "Delivery Count by Status";
            chart.SetData(items);
            FillGrid(new[] { "Delivery Status", "Count" }, rows);
            BuildReportText("Delivery Status Breakdown", new[] { "Delivery Status", "Count" }, rows,
                "Total deliveries: " + DataStore.Deliveries.Count + "   Reply acknowledged: " + acked);
        }

        // -------------------------------------------------------------
        //  Helpers
        // -------------------------------------------------------------
        private Panel Kpi(string label, string value, Color accent)
        {
            var card = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Surface, Margin = new Padding(0, 0, 12, 0) };
            card.Controls.Add(new Panel { Dock = DockStyle.Left, Width = 5, BackColor = accent });
            card.Controls.Add(new Label
            {
                Text = value, Location = new Point(16, 12), AutoSize = true,
                Font = new Font(UiTheme.FontFamily, 17F, FontStyle.Bold), ForeColor = accent
            });
            card.Controls.Add(new Label
            {
                Text = label, Location = new Point(16, 48), AutoSize = true,
                Font = UiTheme.FontSmall, ForeColor = UiTheme.TextMuted
            });
            return card;
        }

        private void SetKpis(params Panel[] cards)
        {
            pnlKpis.SuspendLayout();
            pnlKpis.Controls.Clear();
            pnlKpis.ColumnStyles.Clear();
            pnlKpis.ColumnCount = cards.Length;
            for (int i = 0; i < cards.Length; i++)
            {
                pnlKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / cards.Length));
                pnlKpis.Controls.Add(cards[i], i, 0);
            }
            // Last card has no right margin so the row fills edge-to-edge.
            if (cards.Length > 0)
                cards[cards.Length - 1].Margin = new Padding(0, 0, 0, 0);
            pnlKpis.ResumeLayout();
        }

        private void FillGrid(string[] headers, List<string[]> rows)
        {
            grid.Columns.Clear();
            grid.Rows.Clear();
            foreach (var h in headers) grid.Columns.Add(h, h);
            foreach (var r in rows) grid.Rows.Add(r);
            UiTheme.AlignNumericColumns(grid);
        }

        private void BuildReportText(string title, string[] headers, List<string[]> rows, string summary)
        {
            var sb = new StringBuilder();
            sb.AppendLine("PREMIUM LIVING FURNITURE CO. LTD.");
            sb.AppendLine("STATISTICAL REPORT - " + title);
            sb.AppendLine("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
            if (chkDateRange != null && chkDateRange.Checked)
                sb.AppendLine("Order date range: " + dtpFrom.Value.ToString("yyyy-MM-dd") + " to " + dtpTo.Value.ToString("yyyy-MM-dd"));
            sb.AppendLine(new string('-', 60));

            // Column widths
            int[] w = new int[headers.Length];
            for (int i = 0; i < headers.Length; i++) w[i] = headers[i].Length;
            foreach (var r in rows)
                for (int i = 0; i < r.Length && i < w.Length; i++)
                    if (r[i].Length > w[i]) w[i] = r[i].Length;

            sb.AppendLine(Row(headers, w));
            sb.AppendLine(new string('-', 60));
            foreach (var r in rows) sb.AppendLine(Row(r, w));
            sb.AppendLine(new string('-', 60));
            sb.AppendLine(summary);
            _lastReportText = sb.ToString();
        }

        private static string Row(string[] cells, int[] w)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < cells.Length; i++)
            {
                sb.Append(cells[i].PadRight(i < w.Length ? w[i] + 3 : cells[i].Length + 3));
            }
            return sb.ToString().TrimEnd();
        }

        private void PreviewReport()
        {
            using (var prev = new Form())
            {
                prev.Text = "Report Preview - " + (cmbReport.SelectedItem as string ?? "");
                prev.ClientSize = new Size(640, 520);
                prev.StartPosition = FormStartPosition.CenterParent;
                var tb = new TextBox
                {
                    Multiline = true, ReadOnly = true, Dock = DockStyle.Fill,
                    Font = new Font("Consolas", 10F), ScrollBars = ScrollBars.Both,
                    WordWrap = false, Text = _lastReportText
                };
                prev.Controls.Add(tb);
                prev.ShowDialog(this);
            }
        }

        // =============================================================
        //  Self-drawn bar chart panel (no external dependency)
        // =============================================================
        private class ChartPanel : Panel
        {
            private List<ChartItem> _data = new List<ChartItem>();

            public ChartPanel()
            {
                DoubleBuffered = true;
                ResizeRedraw = true;
            }

            public void SetData(List<ChartItem> data)
            {
                _data = data ?? new List<ChartItem>();
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                int marginLeft = 12, marginRight = 16, marginTop = 12, marginBottom = 40;
                var plot = new Rectangle(marginLeft, marginTop,
                    Width - marginLeft - marginRight, Height - marginTop - marginBottom);

                if (_data == null || _data.Count == 0)
                {
                    using (var br = new SolidBrush(UiTheme.TextMuted))
                    using (var f = new Font(UiTheme.FontFamily, 10F))
                        g.DrawString("No data to display.", f, br, plot.Left + 8, plot.Top + 8);
                    return;
                }

                // Baseline
                using (var axisPen = new Pen(UiTheme.BorderLight))
                    g.DrawLine(axisPen, plot.Left, plot.Bottom, plot.Right, plot.Bottom);

                decimal max = _data.Max(d => d.Value);
                if (max <= 0) max = 1;

                int n = _data.Count;
                float slot = (float)plot.Width / n;
                float barW = Math.Min(slot * 0.6f, 90f);

                using (var lblFont = new Font(UiTheme.FontFamily, 8F))
                using (var valFont = new Font(UiTheme.FontFamily, 8F, FontStyle.Bold))
                using (var lblBrush = new SolidBrush(UiTheme.TextMuted))
                using (var valBrush = new SolidBrush(UiTheme.TextPrimary))
                {
                    var fmt = new StringFormat { Alignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
                    for (int i = 0; i < n; i++)
                    {
                        var d = _data[i];
                        float cx = plot.Left + slot * i + slot / 2f;
                        float h = (float)((double)d.Value / (double)max) * (plot.Height - 18);
                        if (h < 2 && d.Value > 0) h = 2;
                        var barRect = new RectangleF(cx - barW / 2f, plot.Bottom - h, barW, h);

                        using (var br = new SolidBrush(d.Color))
                            g.FillRectangle(br, barRect);

                        // Value on top of the bar
                        string vt = d.ValueText ?? d.Value.ToString();
                        g.DrawString(vt, valFont, valBrush,
                            new RectangleF(cx - slot / 2f, barRect.Top - 16, slot, 14), fmt);

                        // Category label below baseline
                        g.DrawString(d.Label ?? "", lblFont, lblBrush,
                            new RectangleF(cx - slot / 2f, plot.Bottom + 4, slot, 32), fmt);
                    }
                }
            }
        }
    }
}
