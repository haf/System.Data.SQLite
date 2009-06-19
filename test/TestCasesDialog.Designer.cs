namespace test
{
  partial class TestCasesDialog
  {
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
      if (disposing && (components != null))
      {
        components.Dispose();
      }
      base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
      System.Windows.Forms.Label label1;
      System.Windows.Forms.Label label2;
      System.Windows.Forms.Button closeButton;
      System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
      System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
      this._provider = new System.Windows.Forms.ComboBox();
      this._connectionString = new System.Windows.Forms.ComboBox();
      this._grid = new System.Windows.Forms.DataGridView();
      this.Test = new System.Windows.Forms.DataGridViewTextBoxColumn();
      this.Result = new System.Windows.Forms.DataGridViewTextBoxColumn();
      this.Time = new System.Windows.Forms.DataGridViewTextBoxColumn();
      this.Information = new System.Windows.Forms.DataGridViewTextBoxColumn();
      this.runButton = new System.Windows.Forms.Button();
      this.menuStrip1 = new System.Windows.Forms.MenuStrip();
      this.testMenu = new System.Windows.Forms.ToolStripMenuItem();
      label1 = new System.Windows.Forms.Label();
      label2 = new System.Windows.Forms.Label();
      closeButton = new System.Windows.Forms.Button();
      ((System.ComponentModel.ISupportInitialize)(this._grid)).BeginInit();
      this.menuStrip1.SuspendLayout();
      this.SuspendLayout();
      // 
      // label1
      // 
      label1.AutoSize = true;
      label1.Location = new System.Drawing.Point(14, 35);
      label1.Name = "label1";
      label1.Size = new System.Drawing.Size(46, 13);
      label1.TabIndex = 0;
      label1.Text = "&Provider";
      // 
      // label2
      // 
      label2.AutoSize = true;
      label2.Location = new System.Drawing.Point(250, 35);
      label2.Name = "label2";
      label2.Size = new System.Drawing.Size(91, 13);
      label2.TabIndex = 2;
      label2.Text = "Connection &String";
      // 
      // closeButton
      // 
      closeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      closeButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
      closeButton.Location = new System.Drawing.Point(534, 514);
      closeButton.Name = "closeButton";
      closeButton.Size = new System.Drawing.Size(75, 23);
      closeButton.TabIndex = 5;
      closeButton.Text = "&Close";
      closeButton.UseVisualStyleBackColor = true;
      closeButton.Click += new System.EventHandler(this.closeButton_Click);
      // 
      // _provider
      // 
      this._provider.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this._provider.FormattingEnabled = true;
      this._provider.Location = new System.Drawing.Point(66, 32);
      this._provider.Name = "_provider";
      this._provider.Size = new System.Drawing.Size(178, 21);
      this._provider.TabIndex = 1;
      // 
      // _connectionString
      // 
      this._connectionString.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this._connectionString.FormattingEnabled = true;
      this._connectionString.Location = new System.Drawing.Point(347, 32);
      this._connectionString.Name = "_connectionString";
      this._connectionString.Size = new System.Drawing.Size(262, 21);
      this._connectionString.TabIndex = 3;
      // 
      // _grid
      // 
      this._grid.AllowUserToAddRows = false;
      this._grid.AllowUserToDeleteRows = false;
      this._grid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                  | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this._grid.BackgroundColor = System.Drawing.SystemColors.Window;
      this._grid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
      this._grid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Test,
            this.Result,
            this.Time,
            this.Information});
      this._grid.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
      this._grid.Location = new System.Drawing.Point(12, 58);
      this._grid.Name = "_grid";
      this._grid.ReadOnly = true;
      this._grid.RowHeadersVisible = false;
      this._grid.Size = new System.Drawing.Size(597, 450);
      this._grid.TabIndex = 4;
      // 
      // Test
      // 
      this.Test.Frozen = true;
      this.Test.HeaderText = "Test";
      this.Test.Name = "Test";
      this.Test.ReadOnly = true;
      this.Test.Width = 150;
      // 
      // Result
      // 
      this.Result.Frozen = true;
      this.Result.HeaderText = "Result";
      this.Result.Name = "Result";
      this.Result.ReadOnly = true;
      this.Result.Resizable = System.Windows.Forms.DataGridViewTriState.True;
      this.Result.Width = 150;
      // 
      // Time
      // 
      dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
      this.Time.DefaultCellStyle = dataGridViewCellStyle3;
      this.Time.HeaderText = "Time (ms)";
      this.Time.Name = "Time";
      this.Time.ReadOnly = true;
      // 
      // Information
      // 
      this.Information.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
      dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
      this.Information.DefaultCellStyle = dataGridViewCellStyle4;
      this.Information.HeaderText = "Information";
      this.Information.Name = "Information";
      this.Information.ReadOnly = true;
      // 
      // runButton
      // 
      this.runButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      this.runButton.Location = new System.Drawing.Point(453, 514);
      this.runButton.Name = "runButton";
      this.runButton.Size = new System.Drawing.Size(75, 23);
      this.runButton.TabIndex = 6;
      this.runButton.Text = "&Run";
      this.runButton.UseVisualStyleBackColor = true;
      this.runButton.Click += new System.EventHandler(this.runButton_Click);
      // 
      // menuStrip1
      // 
      this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.testMenu});
      this.menuStrip1.Location = new System.Drawing.Point(0, 0);
      this.menuStrip1.Name = "menuStrip1";
      this.menuStrip1.Size = new System.Drawing.Size(621, 24);
      this.menuStrip1.TabIndex = 7;
      this.menuStrip1.Text = "menuStrip1";
      // 
      // testMenu
      // 
      this.testMenu.Name = "testMenu";
      this.testMenu.Size = new System.Drawing.Size(46, 20);
      this.testMenu.Text = "&Tests";
      // 
      // TestCasesDialog
      // 
      this.AcceptButton = this.runButton;
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.CancelButton = closeButton;
      this.ClientSize = new System.Drawing.Size(621, 549);
      this.Controls.Add(this.menuStrip1);
      this.Controls.Add(this.runButton);
      this.Controls.Add(closeButton);
      this.Controls.Add(this._grid);
      this.Controls.Add(this._connectionString);
      this.Controls.Add(label2);
      this.Controls.Add(this._provider);
      this.Controls.Add(label1);
      this.MainMenuStrip = this.menuStrip1;
      this.Name = "TestCasesDialog";
      this.Text = "ADO.NET Provider Test";
      ((System.ComponentModel.ISupportInitialize)(this._grid)).EndInit();
      this.menuStrip1.ResumeLayout(false);
      this.menuStrip1.PerformLayout();
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.ComboBox _provider;
    private System.Windows.Forms.ComboBox _connectionString;
    private System.Windows.Forms.DataGridView _grid;
    private System.Windows.Forms.Button runButton;
    private System.Windows.Forms.DataGridViewTextBoxColumn Test;
    private System.Windows.Forms.DataGridViewTextBoxColumn Result;
    private System.Windows.Forms.DataGridViewTextBoxColumn Time;
    private System.Windows.Forms.DataGridViewTextBoxColumn Information;
    private System.Windows.Forms.MenuStrip menuStrip1;
    private System.Windows.Forms.ToolStripMenuItem testMenu;
  }
}