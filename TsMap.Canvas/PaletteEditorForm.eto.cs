using Eto.Forms;

namespace TsMap.Canvas
{
    partial class PaletteEditorForm
    {
        private void InitializeComponent()
        {
            this.label1 = new Label();
            // 
            // label1
            // 
            this.label1.TabIndex = 0;
            this.label1.Text = "Welp, ran out of time so there\'s nothing here yet... Sorry :(";
            // 
            // PaletteEditorForm
            // 
            this.Title = "Palette Editor";
            this.Content = label1;
            this.Padding = 5;
        }

        private Label label1;
    }
}