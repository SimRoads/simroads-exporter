using Eto.Forms;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TsMap.Common;

namespace TsMap.Canvas
{
    public partial class DlcGuardForm : Form
    {
        private ObservableCollection<DlcGuard> dlcGuards = new ObservableCollection<DlcGuard>();

        public DlcGuardForm(List<DlcGuard> dlcGuards)
        {
            InitializeComponent();
            this.dlcGuards.Clear();
            dlcGuards.ForEach(dlc => this.dlcGuards.Add(dlc)); 
        }

        public delegate void UpdateDlcGuardsEvent();
        public UpdateDlcGuardsEvent UpdateDlcGuards;

        private void DlcGuardCheckedListBox_ItemCheck(object sender, EventArgs e)
        {
            foreach (var item in DlcGuardCheckedListBox.SelectedValues)
            {
                ((DlcGuard)item).Enabled = true;
            }

            UpdateDlcGuards.Invoke();
        }
    }
}