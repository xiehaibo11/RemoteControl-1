using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace RemoteControl.Server
{
    public partial class FrmMain
    {
        private readonly Dictionary<int, WorkspacePopupState> workspacePopups = new Dictionary<int, WorkspacePopupState>();

        private void ShowWorkspacePopup(int pageIndex)
        {
            if (this.tabControl1 == null || pageIndex < 0 || pageIndex >= this.tabControl1.TabPages.Count)
                return;

            WorkspacePopupState existingState;
            if (workspacePopups.TryGetValue(pageIndex, out existingState) && existingState.Form != null && !existingState.Form.IsDisposed)
            {
                if (existingState.Form.WindowState == FormWindowState.Minimized)
                    existingState.Form.WindowState = FormWindowState.Normal;
                existingState.Form.Activate();
                existingState.Form.BringToFront();
                return;
            }

            TabPage page = this.tabControl1.TabPages[pageIndex];
            WorkspacePopupState state = new WorkspacePopupState();
            state.TabPage = page;
            state.Controls = CapturePageControls(page);

            Form popup = new Form();
            popup.Text = APP_TITLE + " - " + page.Text;
            popup.StartPosition = FormStartPosition.CenterParent;
            popup.Size = GetWorkspacePopupSize(pageIndex);
            popup.MinimumSize = new Size(760, 420);
            popup.BackColor = page.BackColor;
            popup.ShowIcon = true;
            popup.Icon = this.Icon;

            Panel hostPanel = new Panel();
            hostPanel.Dock = DockStyle.Fill;
            hostPanel.BackColor = page.BackColor;
            hostPanel.Padding = page.Padding;
            popup.Controls.Add(hostPanel);

            for (int i = 0; i < state.Controls.Count; i++)
            {
                WorkspaceControlState controlState = state.Controls[i];
                hostPanel.Controls.Add(controlState.Control);
            }

            for (int i = 0; i < state.Controls.Count; i++)
            {
                WorkspaceControlState controlState = state.Controls[i];
                hostPanel.Controls.SetChildIndex(controlState.Control, controlState.Index);
            }

            state.Form = popup;
            state.HostPanel = hostPanel;
            workspacePopups[pageIndex] = state;

            popup.FormClosed += delegate
            {
                RestoreWorkspacePopup(pageIndex);
            };

            popup.Show(this);
            popup.Activate();
        }

        private List<WorkspaceControlState> CapturePageControls(TabPage page)
        {
            List<WorkspaceControlState> controls = new List<WorkspaceControlState>();
            for (int i = 0; i < page.Controls.Count; i++)
            {
                Control control = page.Controls[i];
                int index = page.Controls.GetChildIndex(control);
                controls.Add(new WorkspaceControlState(control, index));
            }

            for (int i = 0; i < controls.Count; i++)
            {
                page.Controls.Remove(controls[i].Control);
            }

            return controls;
        }

        private void RestoreWorkspacePopup(int pageIndex)
        {
            WorkspacePopupState state;
            if (!workspacePopups.TryGetValue(pageIndex, out state))
                return;

            workspacePopups.Remove(pageIndex);
            if (state.TabPage == null || state.TabPage.IsDisposed)
                return;

            for (int i = 0; i < state.Controls.Count; i++)
            {
                WorkspaceControlState controlState = state.Controls[i];
                Control control = controlState.Control;
                if (control == null || control.IsDisposed)
                    continue;

                if (control.Parent != null)
                    control.Parent.Controls.Remove(control);

                state.TabPage.Controls.Add(control);
            }

            for (int i = 0; i < state.Controls.Count; i++)
            {
                WorkspaceControlState controlState = state.Controls[i];
                Control control = controlState.Control;
                if (control == null || control.IsDisposed || control.Parent != state.TabPage)
                    continue;

                state.TabPage.Controls.SetChildIndex(control, controlState.Index);
            }
        }

        private static Size GetWorkspacePopupSize(int pageIndex)
        {
            if (pageIndex == 1)
                return new Size(1040, 520);

            return new Size(1040, 650);
        }

        private class WorkspacePopupState
        {
            public TabPage TabPage;
            public Form Form;
            public Panel HostPanel;
            public List<WorkspaceControlState> Controls;
        }

        private class WorkspaceControlState
        {
            public WorkspaceControlState(Control control, int index)
            {
                Control = control;
                Index = index;
            }

            public Control Control;
            public int Index;
        }
    }
}
