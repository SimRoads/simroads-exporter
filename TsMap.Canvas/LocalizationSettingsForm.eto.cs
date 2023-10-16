using Eto.Drawing;
using Eto.Forms;
using System;

namespace TsMap.Canvas
{
    partial class LocalizationSettingsForm : Form
    {
        private void InitializeComponent()
        {
            this.SubmitBtn = new Button();
            this.localizationComboBox1 = new ComboBox();
            this.localizationComboBox1.Size = new Size(200, 25);

            // 
            // SubmitBtn
            //
            this.SubmitBtn.Text = "Submit";
            this.SubmitBtn.Click += new EventHandler<EventArgs>(this.SubmitBtn_Click);
            // 
            // localizationComboBox1
            // 
            this.localizationComboBox1.TabIndex = 0;
            // 
            // LocalizationSettingsForm
            // 
            this.Title = "Localization Settings";
            this.Content = new StackLayout
            {
                Spacing = 5,
                Padding = 5,
                Items =
                {
                    localizationComboBox1,
                    SubmitBtn
                }
            };
        }

        private Button SubmitBtn;
        private ComboBox localizationComboBox1;
    }
}