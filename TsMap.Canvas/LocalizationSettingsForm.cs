using Eto.Forms;
using System;
using System.Collections.Generic;

namespace TsMap.Canvas
{
    public partial class LocalizationSettingsForm : Form
    {

        public delegate void UpdateLocalizationEvent(string localeName);

        public UpdateLocalizationEvent UpdateLocalization;

        public LocalizationSettingsForm(List<string> localizationList, string locale)
        {
            InitializeComponent();
            localizationComboBox1.DataStore = localizationList;
            var index = localizationList.FindIndex(x => x == locale);
            localizationComboBox1.SelectedIndex = (index != -1) ? index : 0;
        }

        private void SubmitBtn_Click(object sender, EventArgs e)
        {
            UpdateLocalization(localizationComboBox1.SelectedValue.ToString());
        }
    }
}
