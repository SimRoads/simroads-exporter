using Eto.Forms;
using Eto.Drawing;
using System;

namespace TsMap.Canvas
{
    partial class SetupForm : Form
	{
		void InitializeComponent()
        { 
            this.GameFolderBrowserDialog = new SelectFolderDialog();
            this.NextBtn = new Button();
            this.loadMods = new CheckBox();
            this.BrowseBtn = new Button();
            this.SelectedGamePathLabel = new Label();
            this.SelectedModPathLabel = new Label();
            this.ToBottom = new Button();
            this.InverseSelection = new Button();
            this.ToTop = new Button();
            this.PrioDown = new Button();
            this.SelectAll = new Button();
            this.PrioUp = new Button();
            this.modList = new GridView<Mod>() { DataStore = this._mods};
            this.BrowseModBtn = new Button();
            this.ModFolderBrowserDialog = new SelectFolderDialog();

            // 
            // GameFolderBrowserDialog
            // 
            this.GameFolderBrowserDialog.Title = "Please select the game directory\nE.g. D:/Games/steamapps/common/Euro Truck Simulator 2/";
            this.GameFolderBrowserDialog.Directory = Environment.SpecialFolder.MyComputer.ToString();

            //
            // ModFolderBrowserDialog
            //
            this.ModFolderBrowserDialog.Title = "Please select the mod directory\nE.g. D:/Users/Dario/Documents/Euro Truck Simulator 2/mod";
            this.ModFolderBrowserDialog.Directory = Environment.SpecialFolder.MyComputer.ToString();

            // 
            // NextBtn
            // 
            this.NextBtn.Enabled = false;
            this.NextBtn.TabIndex = 0;
            this.NextBtn.Text = "Continue";
            this.NextBtn.Click += new EventHandler<EventArgs>(this.NextBtn_Click);

            // 
            // loadMods
            // 
            this.loadMods.TabIndex = 2;
            this.loadMods.Text = "Load mods";
            this.loadMods.CheckedChanged += new EventHandler<EventArgs>(this.loadMods_CheckedChanged);

            // 
            // BrowseBtn
            // 
            this.BrowseBtn.TabIndex = 0;
            this.BrowseBtn.Text = "Browse";
            this.BrowseBtn.Click += new EventHandler<EventArgs>(this.BrowseBtn_Click);

            // 
            // SelectedGamePathLabel
            // 
            this.SelectedGamePathLabel.TabIndex = 1;
            this.SelectedGamePathLabel.Text = "Select the game dir.";

            // 
            // SelectedModPathLabel
            // 
            this.SelectedModPathLabel.TabIndex = 1;
            this.SelectedModPathLabel.Text = "Select the mod dir.";

            // 
            // ToBottom
            // 
            this.ToBottom.TabIndex = 3;
            this.ToBottom.Text = "Bottom";
            this.ToBottom.Click += new EventHandler<EventArgs>(this.ToBottom_Click);

            // 
            // InverseSelection
            // 
            this.InverseSelection.TabIndex = 3;
            this.InverseSelection.Text = "Inverse Enabled";
            this.InverseSelection.Click += new EventHandler<EventArgs>(this.InverseSelection_Click);

            // 
            // ToTop
            // 
            this.ToTop.TabIndex = 3;
            this.ToTop.Text = "Top";
            this.ToTop.Click += new EventHandler<EventArgs>(this.ToTop_Click);

            // 
            // PrioDown
            // 
            this.PrioDown.TabIndex = 2;
            this.PrioDown.Text = "Decrease Priority";
            this.PrioDown.Click += new EventHandler<EventArgs>(this.PrioDown_Click);

            // 
            // SelectAll
            // 
            this.SelectAll.TabIndex = 2;
            this.SelectAll.Text = "Enable All";
            this.SelectAll.Click += new EventHandler<EventArgs>(this.CheckAll_Click);

            // 
            // PrioUp
            // 
            this.PrioUp.TabIndex = 2;
            this.PrioUp.Text = "Increase Priority";
            this.PrioUp.Click += new EventHandler<EventArgs>(this.PrioUp_Click);

            // 
            // modList
            // 
            this.modList.TabIndex = 1;
            this.modList.AllowMultipleSelection = false;
            this.modList.AllowEmptySelection = false;
            this.modList.Columns.Add(new GridColumn
            {
                DataCell = new CheckBoxCell { Binding = Binding.Property<Mod, bool?>(r => r.Load) },
                HeaderText = "Enabled",
                Editable = true
            });
            this.modList.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<Mod, string>(r => r.ToString()) },
                HeaderText = "Mod"
            });

            // 
            // BrowseModBtn
            // 
            this.BrowseModBtn.TabIndex = 0;
            this.BrowseModBtn.Text = "Browse Mod Folder";
            this.BrowseModBtn.Click += new EventHandler<EventArgs>(this.BrowseModBtn_Click);

            this.modPanel = new DynamicLayout();
            this.modPanel.Visible = false;
            this.modPanel.BeginVertical(5, new Size(5,5));
            this.modPanel.Add(SelectedModPathLabel);
            this.modPanel.Add(BrowseModBtn);
            this.modPanel.Add(modList);
            this.modPanel.EndBeginVertical(5, new Size(5, 5));
            this.modPanel.BeginHorizontal();
            this.modPanel.Add(SelectAll);
            this.modPanel.Add(InverseSelection);
            this.modPanel.EndBeginHorizontal();
            this.modPanel.Add(PrioUp);
            this.modPanel.Add(ToTop);
            this.modPanel.EndBeginHorizontal(false);
            this.modPanel.Add(PrioDown, true);
            this.modPanel.Add(ToBottom);
            this.modPanel.EndHorizontal();
            this.modPanel.EndVertical();
            this.modPanel.Padding = 10;

            Title = "Setup Form";
            Padding = 5;

            Content = new TableLayout
            {
                Rows =
                {
                    SelectedGamePathLabel,
                    BrowseBtn,
                    loadMods,
                    modPanel,
                    NextBtn,
                },
                Spacing = new Size(5, 5)
			};

		}

        private SelectFolderDialog GameFolderBrowserDialog;
        private Button NextBtn;
        private CheckBox loadMods;
        private Button BrowseBtn;
        private Label SelectedGamePathLabel;
        private DynamicLayout modPanel;
        private GridView<Mod> modList;
        private Button BrowseModBtn;
        private Button PrioDown;
        private Button PrioUp;
        private SelectFolderDialog ModFolderBrowserDialog;
        private Button ToBottom;
        private Button ToTop;
        private Label SelectedModPathLabel;
        private Button InverseSelection;
        private Button SelectAll;

    }
}
