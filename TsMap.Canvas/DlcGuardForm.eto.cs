using Eto.Drawing;
using Eto.Forms;
using System;

namespace TsMap.Canvas
{
    partial class DlcGuardForm : Form
    {
        private void InitializeComponent()
        {
            this.DlcGuardCheckedListBox = new CheckBoxList() { DataStore = this.dlcGuards };
            this.DlcGuardCheckedListBox.Size = new Size(204, 244);
            this.DlcGuardCheckedListBox.TabIndex = 0;
            this.DlcGuardCheckedListBox.SelectedKeysChanged += new EventHandler<EventArgs>(this.DlcGuardCheckedListBox_ItemCheck);

            this.Title = "DLC Guards";
            this.Content = DlcGuardCheckedListBox;
            this.Padding = 5;
        }


        private CheckBoxList DlcGuardCheckedListBox;
    }
}