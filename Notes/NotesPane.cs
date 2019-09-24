using AGS.Types;
using System;
using System.Text;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Reflection;

namespace AGS.Plugin.Notes
{
	public partial class NotesPane : EditorContentPanel
	{
		private IAGSEditor editor;
		private NotesComponent parent;

		public NotesPane(IAGSEditor editor, NotesComponent parent)
		{
			InitializeComponent();
			this.editor = editor;
			this.parent = parent;
		}

		public IScriptEditorControl GetEditor()
		{
            Scintilla.ScintillaControl inner;

			if (scintilla == null)
			{
                // Alternatively, should I do this in the constructor?
				scintilla = editor.GUIController.CreateScriptEditor(new Point(0, 0), new Size());
				scintilla.SetKeyWords("");
				scintilla.AutoCompleteEnabled = false;
				scintilla.ShowLineNumbers();
				scintilla.AutoSpaceAfterComma = false;
				scintilla.CallTipsEnabled = false;
                inner = GetScintillaControl();
                inner.TextModified += new EventHandler<Scintilla.TextModifiedEventArgs>(Control_TextModified);
                inner.SetLexer(Scintilla.Enums.Lexer.Null);
                /*
                inner.SetMarginType(0, Scintilla.Enums.MarginType.Back);
                inner.SetMarginType(1, Scintilla.Enums.MarginType.Back);
                 * */
                inner.SetMarginWidth(2, 0);
                inner.SetMarginWidth(3, 16);
                inner.SetMarginType(3, Scintilla.Enums.MarginType.Back);
                ((System.Windows.Forms.Control)scintilla).Dock = DockStyle.Fill;
				this.SuspendLayout();
				this.Controls.Add((System.Windows.Forms.Control)scintilla);
				this.ResumeLayout(false);
				this.PerformLayout();
			}

			return scintilla;
		}

        public Scintilla.ScintillaControl GetScintillaControl()
        {
            return ((Scintilla.ScintillaControl)scintilla.GetType().GetField("scintillaControl1", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(scintilla));
        }

		private void Control_TextModified(object source, Scintilla.TextModifiedEventArgs e)
		{
			parent.OnTextChanged(this);
		}

		protected override void OnPanelClosing(bool canCancel, ref bool cancelClose)
		{
			parent.OnPanelClosing(this, canCancel, ref cancelClose);
		}

        protected override void OnWindowActivated()
        {
            parent.OnWindowActivated(this);
        }
	}
}
