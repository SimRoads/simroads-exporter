using Eto.Forms;
using System.IO;
using System;
using System.Linq;
using System.Collections.ObjectModel;

namespace TsMap.Canvas
{
	public partial class SetupForm : Form
	{
        private string gamePath;
        private string modPath;
        private ObservableCollection<Mod> _mods = new ObservableCollection<Mod>();

        public Settings AppSettings { get; }

        public SetupForm()
        {
            InitializeComponent();
            AppSettings = JsonHelper.LoadSettings();
            if (AppSettings.LastGamePath != null)
            {
                gamePath = AppSettings.LastGamePath;
                SelectedGamePath();
            }

            if (AppSettings.LastModPath != null)
            {
                modPath = AppSettings.LastModPath;
                SelectedModPath();
            }
        }

        private void SelectedGamePath()
        {
            if (!Directory.Exists(gamePath)) return;
            SelectedGamePathLabel.Text = AppSettings.LastGamePath = GameFolderBrowserDialog.Directory = gamePath;
            if (loadMods.Checked == true && modPath == null) return;
            NextBtn.Enabled = true;
        }

        private void SelectedModPath()
        {
            if (!Directory.Exists(modPath)) return;
            SelectedModPathLabel.Text = AppSettings.LastModPath = ModFolderBrowserDialog.Directory = modPath;
            var files = Directory.EnumerateFiles(modPath, "*.*", SearchOption.TopDirectoryOnly)
                .Where(s => s.EndsWith(".scs", StringComparison.OrdinalIgnoreCase) ||
                            s.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
            _mods.Clear();
            foreach (var x in files)
            {
                _mods.Add(new Mod(x));
            }
            UpdateModList();
            if (gamePath != null) NextBtn.Enabled = true;
        }

        private void BrowseBtn_Click(object sender, EventArgs e)
        {
            var result = GameFolderBrowserDialog.ShowDialog(this);
            if (result == DialogResult.Ok)
            {
                gamePath = GameFolderBrowserDialog.Directory;
                SelectedGamePath();
            }
        }

        private void NextBtn_Click(object sender, EventArgs e)
        {
            new TsMapCanvas(this, gamePath, _mods.ToList()).Show();
            this.Visible = false;
        }

        private void loadMods_CheckedChanged(object sender, EventArgs e)
        {
            if (loadMods.Checked == true)
            {
                modPanel.Visible = true;
                if (modPath == null) NextBtn.Enabled = false;
                UpdateModList();
                this.Size = new Eto.Drawing.Size(-1, 400);
            }
            else
            {
                modPanel.Visible = false;
                foreach (var item in _mods)
                {
                    item.Load = false;
                }
                if (gamePath != null) NextBtn.Enabled = true;
            }
        }

        private void UpdateModList()
        {
            var selectedIndex = modList.SelectedRow;
            modList.SelectedRow = selectedIndex;
            modList.Invalidate();
        }

        private void MoveItemToTop(int index)
        {
            if (index < 1 || index > _mods.Count - 1) return;
            var newTopMod = _mods[index];
            for (var i = index; i > 0; i--)
            {
                _mods[i] = _mods[i - 1];
            }

            _mods[0] = newTopMod;
            modList.SelectedRow = 0;
            UpdateModList();
        }

        private void MoveItemToBottom(int index)
        {
            if (index > _mods.Count - 2 || index < 0) return;
            var newBottomMod = _mods[index];
            for (var i = index; i < _mods.Count - 1; i++)
            {
                _mods[i] = _mods[i + 1];
            }

            _mods[_mods.Count - 1] = newBottomMod;
            modList.SelectedRow = _mods.Count - 1;
            UpdateModList();
        }

        private void MoveItem(int index, int direction)
        {
            if (index < 0) return;
            var newIndex = index + direction;
            if (newIndex < 0 || newIndex >= _mods.Count) return;
            var origItem = _mods[newIndex];
            _mods[newIndex] = _mods[index];
            _mods[index] = origItem;
            modList.SelectedRow = modList.SelectedRow + direction;
            UpdateModList();
        }

        private void PrioUp_Click(object sender, EventArgs e)
        {
            if (_mods.Count > 1) MoveItem(modList.SelectedRow, -1);
        }

        private void PrioDown_Click(object sender, EventArgs e)
        {
            if (_mods.Count > 1) MoveItem(modList.SelectedRow, 1);
        }

        private void BrowseModBtn_Click(object sender, EventArgs e)
        {
            var result = ModFolderBrowserDialog.ShowDialog(this);
            if (result == DialogResult.Ok)
            {
                modPath = ModFolderBrowserDialog.Directory;
                SelectedModPath();
            }
        }


        private void ToTop_Click(object sender, EventArgs e)
        {
            MoveItemToTop(modList.SelectedRow);
        }

        private void ToBottom_Click(object sender, EventArgs e)
        {
            MoveItemToBottom(modList.SelectedRow);
        }

        private void InverseSelection_Click(object sender, EventArgs e)
        {
            foreach (var item in _mods)
            {
                item.Load = !item.Load;
            }
            UpdateModList();
        }

        private void CheckAll_Click(object sender, EventArgs e)
        {
            foreach (var item in _mods)
            {
                item.Load = true;
            }
            UpdateModList();
        }

	}
}
