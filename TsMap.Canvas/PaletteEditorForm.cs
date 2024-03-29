﻿using Eto.Forms;
using TsMap.Canvas.Renderer;

namespace TsMap.Canvas
{
    public partial class PaletteEditorForm : Form
    {
        public delegate void UpdatePaletteEvent(MapPalette palette);
        public UpdatePaletteEvent UpdatePalette;

        public PaletteEditorForm(MapPalette palette)
        {
            InitializeComponent();
        }
    }
}
