namespace SQLite.Designer
{
  partial class ChangeScriptDialog
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
      System.Windows.Forms.PictureBox pictureBox1;
      System.Windows.Forms.Label label1;
      System.Windows.Forms.Button noButton;
      System.Windows.Forms.Button yesButton;
      this._script = new System.Windows.Forms.RichTextBox();
      pictureBox1 = new System.Windows.Forms.PictureBox();
      label1 = new System.Windows.Forms.Label();
      noButton = new System.Windows.Forms.Button();
      yesButton = new System.Windows.Forms.Button();
      ((System.ComponentModel.ISupportInitialize)(pictureBox1)).BeginInit();
      this.SuspendLayout();
      // 
      // pictureBox1
      // 
      pictureBox1.Image = global::SQLite.Designer.VSPackage.info;
      pictureBox1.Location = new System.Drawing.Point(13, 13);
      pictureBox1.Name = "pictureBox1";
      pictureBox1.Size = new System.Drawing.Size(48, 48);
      pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
      pictureBox1.TabIndex = 0;
      pictureBox1.TabStop = false;
      // 
      // label1
      // 
      label1.AutoSize = true;
      label1.Location = new System.Drawing.Point(67, 31);
      label1.Name = "label1";
      label1.Size = new System.Drawing.Size(200, 13);
      label1.TabIndex = 1;
      label1.Text = "Do you want to save this script to a file?";
      // 
      // noButton
      // 
      noButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
      noButton.Location = new System.Drawing.Point(445, 362);
      noButton.Name = "noButton";
      noButton.Size = new System.Drawing.Size(75, 25);
      noButton.TabIndex = 3;
      noButton.Text = "&No";
      noButton.UseVisualStyleBackColor = true;
      noButton.Click += new System.EventHandler(this.noButton_Click);
      // 
      // yesButton
      // 
      yesButton.Location = new System.Drawing.Point(364, 362);
      yesButton.Name = "yesButton";
      yesButton.Size = new System.Drawing.Size(75, 25);
      yesButton.TabIndex = 4;
      yesButton.Text = "&Yes";
      yesButton.UseVisualStyleBackColor = true;
      yesButton.Click += new System.EventHandler(this.yesButton_Click);
      // 
      // _script
      // 
      this._script.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
      this._script.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this._script.Location = new System.Drawing.Point(12, 76);
      this._script.Name = "_script";
      this._script.ReadOnly = true;
      this._script.Size = new System.Drawing.Size(508, 280);
      this._script.TabIndex = 2;
      this._script.Text = "";
      this._script.WordWrap = false;
      // 
      // ChangeScriptDialog
      // 
      this.AcceptButton = yesButton;
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.CancelButton = noButton;
      this.ClientSize = new System.Drawing.Size(532, 399);
      this.Controls.Add(yesButton);
      this.Controls.Add(noButton);
      this.Controls.Add(this._script);
      this.Controls.Add(label1);
      this.Controls.Add(pictureBox1);
      this.Font = new System.Drawing.Font("MS Shell Dlg 2", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
      this.MaximizeBox = false;
      this.MinimizeBox = false;
      this.Name = "ChangeScriptDialog";
      this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
      this.Text = "Save Change Script";
      ((System.ComponentModel.ISupportInitialize)(pictureBox1)).EndInit();
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.RichTextBox _script;

  }
}