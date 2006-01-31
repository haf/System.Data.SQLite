namespace SQLite.Designer
{
  partial class SQLiteConnectionUIControl
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
      System.Windows.Forms.Label labelPassword;
      System.Windows.Forms.GroupBox securityGroup;
      System.Windows.Forms.Label labelConfirm;
      System.Windows.Forms.GroupBox encodingGroup;
      System.Windows.Forms.GroupBox dateTimeGroup;
      System.Windows.Forms.GroupBox databaseGroup;
      System.Windows.Forms.Label cacheSizeLabel;
      System.Windows.Forms.Label pageSizeLabel;
      System.Windows.Forms.GroupBox syncGroup;
      this.confirmTextBox = new System.Windows.Forms.TextBox();
      this.passwordTextBox = new System.Windows.Forms.TextBox();
      this.utf16RadioButton = new System.Windows.Forms.RadioButton();
      this.utf8RadioButton = new System.Windows.Forms.RadioButton();
      this.ticksRadioButton = new System.Windows.Forms.RadioButton();
      this.iso8601RadioButton = new System.Windows.Forms.RadioButton();
      this.textBox1 = new System.Windows.Forms.TextBox();
      this.pageSizeTextBox = new System.Windows.Forms.TextBox();
      this.fileTextBox = new System.Windows.Forms.TextBox();
      this.browseButton = new System.Windows.Forms.Button();
      this.newDatabase = new System.Windows.Forms.Button();
      this.offRadioButton = new System.Windows.Forms.RadioButton();
      this.normalRadioButton = new System.Windows.Forms.RadioButton();
      this.fullRadioButton = new System.Windows.Forms.RadioButton();
      this.connectButton = new System.Windows.Forms.Button();
      this.cancelButton = new System.Windows.Forms.Button();
      labelPassword = new System.Windows.Forms.Label();
      securityGroup = new System.Windows.Forms.GroupBox();
      labelConfirm = new System.Windows.Forms.Label();
      encodingGroup = new System.Windows.Forms.GroupBox();
      dateTimeGroup = new System.Windows.Forms.GroupBox();
      databaseGroup = new System.Windows.Forms.GroupBox();
      cacheSizeLabel = new System.Windows.Forms.Label();
      pageSizeLabel = new System.Windows.Forms.Label();
      syncGroup = new System.Windows.Forms.GroupBox();
      securityGroup.SuspendLayout();
      encodingGroup.SuspendLayout();
      dateTimeGroup.SuspendLayout();
      databaseGroup.SuspendLayout();
      syncGroup.SuspendLayout();
      this.SuspendLayout();
      // 
      // labelPassword
      // 
      labelPassword.AutoSize = true;
      labelPassword.Location = new System.Drawing.Point(6, 23);
      labelPassword.Name = "labelPassword";
      labelPassword.Size = new System.Drawing.Size(53, 13);
      labelPassword.TabIndex = 0;
      labelPassword.Text = "Password";
      // 
      // securityGroup
      // 
      securityGroup.Controls.Add(this.confirmTextBox);
      securityGroup.Controls.Add(labelConfirm);
      securityGroup.Controls.Add(this.passwordTextBox);
      securityGroup.Controls.Add(labelPassword);
      securityGroup.Location = new System.Drawing.Point(12, 295);
      securityGroup.Name = "securityGroup";
      securityGroup.Size = new System.Drawing.Size(306, 80);
      securityGroup.TabIndex = 10;
      securityGroup.TabStop = false;
      securityGroup.Text = "Encryption";
      // 
      // confirmTextBox
      // 
      this.confirmTextBox.Location = new System.Drawing.Point(65, 47);
      this.confirmTextBox.Name = "confirmTextBox";
      this.confirmTextBox.Size = new System.Drawing.Size(235, 21);
      this.confirmTextBox.TabIndex = 3;
      this.confirmTextBox.UseSystemPasswordChar = true;
      // 
      // labelConfirm
      // 
      labelConfirm.AutoSize = true;
      labelConfirm.Location = new System.Drawing.Point(15, 50);
      labelConfirm.Name = "labelConfirm";
      labelConfirm.Size = new System.Drawing.Size(44, 13);
      labelConfirm.TabIndex = 2;
      labelConfirm.Text = "Confirm";
      // 
      // passwordTextBox
      // 
      this.passwordTextBox.Location = new System.Drawing.Point(65, 20);
      this.passwordTextBox.Name = "passwordTextBox";
      this.passwordTextBox.Size = new System.Drawing.Size(235, 21);
      this.passwordTextBox.TabIndex = 1;
      this.passwordTextBox.UseSystemPasswordChar = true;
      // 
      // encodingGroup
      // 
      encodingGroup.Controls.Add(this.utf16RadioButton);
      encodingGroup.Controls.Add(this.utf8RadioButton);
      encodingGroup.Location = new System.Drawing.Point(12, 191);
      encodingGroup.Name = "encodingGroup";
      encodingGroup.Size = new System.Drawing.Size(75, 98);
      encodingGroup.TabIndex = 7;
      encodingGroup.TabStop = false;
      encodingGroup.Text = "Encoding";
      // 
      // utf16RadioButton
      // 
      this.utf16RadioButton.AutoSize = true;
      this.utf16RadioButton.Location = new System.Drawing.Point(6, 44);
      this.utf16RadioButton.Name = "utf16RadioButton";
      this.utf16RadioButton.Size = new System.Drawing.Size(60, 17);
      this.utf16RadioButton.TabIndex = 1;
      this.utf16RadioButton.TabStop = true;
      this.utf16RadioButton.Text = "UTF-16";
      this.utf16RadioButton.UseVisualStyleBackColor = true;
      // 
      // utf8RadioButton
      // 
      this.utf8RadioButton.AutoSize = true;
      this.utf8RadioButton.Checked = true;
      this.utf8RadioButton.Location = new System.Drawing.Point(7, 21);
      this.utf8RadioButton.Name = "utf8RadioButton";
      this.utf8RadioButton.Size = new System.Drawing.Size(54, 17);
      this.utf8RadioButton.TabIndex = 0;
      this.utf8RadioButton.TabStop = true;
      this.utf8RadioButton.Text = "UTF-8";
      this.utf8RadioButton.UseVisualStyleBackColor = true;
      // 
      // dateTimeGroup
      // 
      dateTimeGroup.Controls.Add(this.ticksRadioButton);
      dateTimeGroup.Controls.Add(this.iso8601RadioButton);
      dateTimeGroup.Location = new System.Drawing.Point(93, 191);
      dateTimeGroup.Name = "dateTimeGroup";
      dateTimeGroup.Size = new System.Drawing.Size(113, 98);
      dateTimeGroup.TabIndex = 8;
      dateTimeGroup.TabStop = false;
      dateTimeGroup.Text = "Date/Time Format";
      // 
      // ticksRadioButton
      // 
      this.ticksRadioButton.AutoSize = true;
      this.ticksRadioButton.Location = new System.Drawing.Point(6, 44);
      this.ticksRadioButton.Name = "ticksRadioButton";
      this.ticksRadioButton.Size = new System.Drawing.Size(48, 17);
      this.ticksRadioButton.TabIndex = 1;
      this.ticksRadioButton.TabStop = true;
      this.ticksRadioButton.Text = "Ticks";
      this.ticksRadioButton.UseVisualStyleBackColor = true;
      // 
      // iso8601RadioButton
      // 
      this.iso8601RadioButton.AutoSize = true;
      this.iso8601RadioButton.Checked = true;
      this.iso8601RadioButton.Location = new System.Drawing.Point(7, 21);
      this.iso8601RadioButton.Name = "iso8601RadioButton";
      this.iso8601RadioButton.Size = new System.Drawing.Size(71, 17);
      this.iso8601RadioButton.TabIndex = 0;
      this.iso8601RadioButton.TabStop = true;
      this.iso8601RadioButton.Text = "ISO-8601";
      this.iso8601RadioButton.UseVisualStyleBackColor = true;
      // 
      // databaseGroup
      // 
      databaseGroup.Controls.Add(cacheSizeLabel);
      databaseGroup.Controls.Add(this.textBox1);
      databaseGroup.Controls.Add(pageSizeLabel);
      databaseGroup.Controls.Add(this.pageSizeTextBox);
      databaseGroup.Controls.Add(this.fileTextBox);
      databaseGroup.Controls.Add(this.browseButton);
      databaseGroup.Controls.Add(this.newDatabase);
      databaseGroup.Location = new System.Drawing.Point(12, 12);
      databaseGroup.Name = "databaseGroup";
      databaseGroup.Size = new System.Drawing.Size(306, 150);
      databaseGroup.TabIndex = 8;
      databaseGroup.TabStop = false;
      databaseGroup.Text = "Database";
      // 
      // cacheSizeLabel
      // 
      cacheSizeLabel.AutoSize = true;
      cacheSizeLabel.Location = new System.Drawing.Point(7, 116);
      cacheSizeLabel.Name = "cacheSizeLabel";
      cacheSizeLabel.Size = new System.Drawing.Size(59, 13);
      cacheSizeLabel.TabIndex = 5;
      cacheSizeLabel.Text = "Cache Size";
      // 
      // textBox1
      // 
      this.textBox1.Location = new System.Drawing.Point(72, 113);
      this.textBox1.Name = "textBox1";
      this.textBox1.Size = new System.Drawing.Size(100, 21);
      this.textBox1.TabIndex = 6;
      this.textBox1.Text = "2000";
      // 
      // pageSizeLabel
      // 
      pageSizeLabel.AutoSize = true;
      pageSizeLabel.Location = new System.Drawing.Point(13, 89);
      pageSizeLabel.Name = "pageSizeLabel";
      pageSizeLabel.Size = new System.Drawing.Size(53, 13);
      pageSizeLabel.TabIndex = 3;
      pageSizeLabel.Text = "Page Size";
      // 
      // pageSizeTextBox
      // 
      this.pageSizeTextBox.Location = new System.Drawing.Point(72, 86);
      this.pageSizeTextBox.Name = "pageSizeTextBox";
      this.pageSizeTextBox.Size = new System.Drawing.Size(100, 21);
      this.pageSizeTextBox.TabIndex = 4;
      this.pageSizeTextBox.Text = "1024";
      // 
      // fileTextBox
      // 
      this.fileTextBox.Location = new System.Drawing.Point(6, 20);
      this.fileTextBox.Name = "fileTextBox";
      this.fileTextBox.Size = new System.Drawing.Size(294, 21);
      this.fileTextBox.TabIndex = 0;
      // 
      // browseButton
      // 
      this.browseButton.Location = new System.Drawing.Point(6, 47);
      this.browseButton.Name = "browseButton";
      this.browseButton.Size = new System.Drawing.Size(75, 23);
      this.browseButton.TabIndex = 1;
      this.browseButton.Text = "&Browse ...";
      this.browseButton.UseVisualStyleBackColor = true;
      this.browseButton.Click += new System.EventHandler(this.browseButton_Click);
      // 
      // newDatabase
      // 
      this.newDatabase.Location = new System.Drawing.Point(87, 47);
      this.newDatabase.Name = "newDatabase";
      this.newDatabase.Size = new System.Drawing.Size(75, 23);
      this.newDatabase.TabIndex = 2;
      this.newDatabase.Text = "&New ...";
      this.newDatabase.UseVisualStyleBackColor = true;
      this.newDatabase.Click += new System.EventHandler(this.newDatabase_Click);
      // 
      // syncGroup
      // 
      syncGroup.Controls.Add(this.offRadioButton);
      syncGroup.Controls.Add(this.normalRadioButton);
      syncGroup.Controls.Add(this.fullRadioButton);
      syncGroup.Location = new System.Drawing.Point(213, 191);
      syncGroup.Name = "syncGroup";
      syncGroup.Size = new System.Drawing.Size(105, 98);
      syncGroup.TabIndex = 9;
      syncGroup.TabStop = false;
      syncGroup.Text = "Synchronization";
      // 
      // offRadioButton
      // 
      this.offRadioButton.AutoSize = true;
      this.offRadioButton.Location = new System.Drawing.Point(6, 66);
      this.offRadioButton.Name = "offRadioButton";
      this.offRadioButton.Size = new System.Drawing.Size(41, 17);
      this.offRadioButton.TabIndex = 2;
      this.offRadioButton.Text = "Off";
      this.offRadioButton.UseVisualStyleBackColor = true;
      // 
      // normalRadioButton
      // 
      this.normalRadioButton.AutoSize = true;
      this.normalRadioButton.Checked = true;
      this.normalRadioButton.Location = new System.Drawing.Point(6, 43);
      this.normalRadioButton.Name = "normalRadioButton";
      this.normalRadioButton.Size = new System.Drawing.Size(58, 17);
      this.normalRadioButton.TabIndex = 1;
      this.normalRadioButton.TabStop = true;
      this.normalRadioButton.Text = "Normal";
      this.normalRadioButton.UseVisualStyleBackColor = true;
      // 
      // fullRadioButton
      // 
      this.fullRadioButton.AutoSize = true;
      this.fullRadioButton.Location = new System.Drawing.Point(6, 20);
      this.fullRadioButton.Name = "fullRadioButton";
      this.fullRadioButton.Size = new System.Drawing.Size(41, 17);
      this.fullRadioButton.TabIndex = 0;
      this.fullRadioButton.Text = "Full";
      this.fullRadioButton.UseVisualStyleBackColor = true;
      // 
      // connectButton
      // 
      this.connectButton.Location = new System.Drawing.Point(162, 388);
      this.connectButton.Name = "connectButton";
      this.connectButton.Size = new System.Drawing.Size(75, 23);
      this.connectButton.TabIndex = 11;
      this.connectButton.Text = "&Connect";
      this.connectButton.UseVisualStyleBackColor = true;
      // 
      // cancelButton
      // 
      this.cancelButton.Location = new System.Drawing.Point(243, 388);
      this.cancelButton.Name = "cancelButton";
      this.cancelButton.Size = new System.Drawing.Size(75, 23);
      this.cancelButton.TabIndex = 12;
      this.cancelButton.Text = "Cancel";
      this.cancelButton.UseVisualStyleBackColor = true;
      // 
      // SQLiteConnectionUIControl
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(330, 423);
      this.Controls.Add(this.cancelButton);
      this.Controls.Add(this.connectButton);
      this.Controls.Add(syncGroup);
      this.Controls.Add(databaseGroup);
      this.Controls.Add(dateTimeGroup);
      this.Controls.Add(encodingGroup);
      this.Controls.Add(securityGroup);
      this.Font = new System.Drawing.Font("MS Shell Dlg 2", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.HelpButton = true;
      this.MaximizeBox = false;
      this.MinimizeBox = false;
      this.Name = "SQLiteConnectionUIControl";
      this.ShowIcon = false;
      this.Text = "SQLite Connection Properties";
      securityGroup.ResumeLayout(false);
      securityGroup.PerformLayout();
      encodingGroup.ResumeLayout(false);
      encodingGroup.PerformLayout();
      dateTimeGroup.ResumeLayout(false);
      dateTimeGroup.PerformLayout();
      databaseGroup.ResumeLayout(false);
      databaseGroup.PerformLayout();
      syncGroup.ResumeLayout(false);
      syncGroup.PerformLayout();
      this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.TextBox fileTextBox;
    private System.Windows.Forms.Button browseButton;
    private System.Windows.Forms.Button newDatabase;
    private System.Windows.Forms.TextBox confirmTextBox;
    private System.Windows.Forms.TextBox passwordTextBox;
    private System.Windows.Forms.RadioButton utf16RadioButton;
    private System.Windows.Forms.RadioButton utf8RadioButton;
    private System.Windows.Forms.RadioButton ticksRadioButton;
    private System.Windows.Forms.RadioButton iso8601RadioButton;
    private System.Windows.Forms.TextBox pageSizeTextBox;
    private System.Windows.Forms.TextBox textBox1;
    private System.Windows.Forms.RadioButton offRadioButton;
    private System.Windows.Forms.RadioButton normalRadioButton;
    private System.Windows.Forms.RadioButton fullRadioButton;
    private System.Windows.Forms.Button connectButton;
    private System.Windows.Forms.Button cancelButton;
  }
}