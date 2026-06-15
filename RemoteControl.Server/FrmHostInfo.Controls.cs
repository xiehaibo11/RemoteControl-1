using System.Drawing;
using System.Windows.Forms;

namespace RemoteControl.Server
{
    public partial class FrmHostInfo
    {
        private ListView CreateDetailsListView()
        {
            var lv = new ListView();
            lv.Dock = DockStyle.Fill;
            lv.View = View.Details;
            lv.FullRowSelect = true;
            lv.GridLines = true;
            return lv;
        }

        private TableLayoutPanel CreateInfoPanel()
        {
            var panel = new TableLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.ColumnCount = 2;
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            panel.Font = new Font("微软雅黑", 9F);
            panel.Padding = new Padding(10, 10, 10, 10);
            return panel;
        }

        private Label CreateEmptyLabel(string text)
        {
            var lbl = new Label();
            lbl.Text = text;
            lbl.Dock = DockStyle.Fill;
            lbl.TextAlign = ContentAlignment.MiddleCenter;
            lbl.Font = new Font("微软雅黑", 9F);
            lbl.ForeColor = SystemColors.GrayText;
            return lbl;
        }

        private void AddInfoRow(TableLayoutPanel panel, string label, string value)
        {
            var lbl = new Label();
            lbl.Text = label;
            lbl.AutoSize = true;
            lbl.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            lbl.Dock = DockStyle.Fill;
            lbl.TextAlign = ContentAlignment.MiddleLeft;

            var val = new Label();
            val.Text = value ?? "";
            val.AutoSize = true;
            val.Font = new Font("微软雅黑", 9F);
            val.Dock = DockStyle.Fill;
            val.TextAlign = ContentAlignment.MiddleLeft;

            panel.RowCount++;
            panel.Controls.Add(lbl, 0, panel.RowCount - 1);
            panel.Controls.Add(val, 1, panel.RowCount - 1);
        }
    }
}
