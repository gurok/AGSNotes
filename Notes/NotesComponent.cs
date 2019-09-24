using AGS.Types;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Timers;
using System.Reflection;
using AGS.Editor;

namespace AGS.Plugin.Notes
{
	public class NotesComponent : IEditorComponent
	{
		private const string ID__BASE = "NotesPlugin";
		private const string ID__SEPARATOR = "::";
		private const string ID__TYPE_ICON = "Icon";
		private const string ID__TYPE_MENU = "Menu";
		private const string ID__TYPE_NODE = "Node";

		private readonly static string ID_ICON_ROOT = QualifyID(ID__TYPE_ICON, "*");
        private readonly static string ID_ICON_LEAF = QualifyID(ID__TYPE_ICON, "Leaf");

        private readonly static string ID_MENU_CONTEXT_NEW_NOTE = QualifyID(ID__TYPE_MENU, "Context - New Note");
        private readonly static string ID_MENU_CONTEXT_RENAME = QualifyID(ID__TYPE_MENU, "Context - Rename");
        private readonly static string ID_MENU_CONTEXT_DELETE = QualifyID(ID__TYPE_MENU, "Context - Delete");

        private readonly static string ID_MENU_EDIT = QualifyID(ID__TYPE_MENU, "Edit");
        private readonly static string ID_MENU_EDIT_UNDO = QualifyID(ID__TYPE_MENU, "Edit - Undo");
        private readonly static string ID_MENU_EDIT_REDO = QualifyID(ID__TYPE_MENU, "Edit - Redo");
        private readonly static string ID_MENU_EDIT_CUT = QualifyID(ID__TYPE_MENU, "Edit - Cut");
        private readonly static string ID_MENU_EDIT_COPY = QualifyID(ID__TYPE_MENU, "Edit - Copy");
        private readonly static string ID_MENU_EDIT_PASTE = QualifyID(ID__TYPE_MENU, "Edit - Paste");
        private readonly static string ID_MENU_EDIT_DELETE = QualifyID(ID__TYPE_MENU, "Edit - Delete");
        private readonly static string ID_MENU_EDIT_FIND = QualifyID(ID__TYPE_MENU, "Edit - Find");

        private readonly static string ID_NODE_ROOT = QualifyID(ID__TYPE_NODE, "*");

		private IAGSEditor editor;
		private List<String> title;
		private List<String> saved;
		private List<NotesPane> pane;
		private List<ContentDocument> document;
		private int selected;
        
        // TODO: Move these to lazy init methods
        private readonly static IList<MenuCommand> contextMenuListGeneral = GetContextMenuListDefault();
        private readonly static IList<MenuCommand> contextMenuListItem = GetContextMenuListItem();

		public NotesComponent(IAGSEditor editor)
		{
			this.editor = editor;
			editor.GUIController.RegisterIcon(ID_ICON_ROOT, Notes.Properties.Resources.PluginIcon);
			editor.GUIController.RegisterIcon(ID_ICON_LEAF, Notes.Properties.Resources.Leaf);
			editor.GUIController.ProjectTree.AddTreeRoot(this, ID_NODE_ROOT, "Notes", ID_ICON_ROOT);
		}

		string IEditorComponent.ComponentID
		{
			get { return "Notes"; }
		}

		private static string QualifyID(string type, string name)
		{
			return (ID__BASE + ID__SEPARATOR + type + ID__SEPARATOR + name);
		}

        private MenuCommand CreateMenuCommand(string id, string title, Keys shortcut, string icon)
        {
            MenuCommand item;

            item = editor.GUIController.CreateMenuCommand(this, id, title);
            item.ShortcutKey = shortcut;
            item.IconKey = icon;

            return (item);
        }

        private MenuCommands GetExtraMenu()
        {
            MenuCommands menu;

            menu = new MenuCommands("&Edit", "fileToolStripMenuItem");
            
            menu.Commands.Add(CreateMenuCommand(ID_MENU_EDIT_UNDO, "&Undo", Keys.Control | Keys.Z, "UndoMenuIcon"));
            /*
            menu.Commands.Add(new MenuCommand(ID_MENU_EDIT_UNDO, "&Undo", Keys.Control | Keys.Z, "UndoMenuIcon"));
            menu.Commands.Add(new MenuCommand(ID_MENU_EDIT_REDO, "&Redo", Keys.Control | Keys.Y, "RedoMenuIcon"));
            menu.Commands.Add(MenuCommand.Separator);
            menu.Commands.Add(new MenuCommand(ID_MENU_EDIT_CUT, "Cu&t", Keys.Control | Keys.X, "CutMenuIcon"));
            menu.Commands.Add(new MenuCommand(ID_MENU_EDIT_COPY, "&Copy", Keys.Control | Keys.C, "CopyMenuIcon"));
            menu.Commands.Add(new MenuCommand(ID_MENU_EDIT_PASTE, "&Paste", Keys.Control | Keys.V, "PasteMenuIcon"));
            menu.Commands.Add(MenuCommand.Separator);
            menu.Commands.Add(new MenuCommand(ID_MENU_EDIT_FIND, "&Find...", Keys.Control | Keys.F, "FindMenuIcon"));
            */

            return (menu);
        }

        private void AbleExtraMenu(NotesPane source)
        {
            ScintillaWrapper wrapper;
            bool canCutAndCopy;
            IList<MenuCommand> list;
            int index;

            index = GetPaneIndex(source);
            if (index > -1 && false)
            {
                wrapper = (ScintillaWrapper)source.GetEditor().Control;
                canCutAndCopy = wrapper.CanCutAndCopy();
                list = document[index].MainMenu.Commands;
                list[0].Enabled = wrapper.CanUndo();
                //list[1].Enabled = wrapper.CanRedo();
                //list[3].Enabled = canCutAndCopy;
                //list[4].Enabled = canCutAndCopy;
                //list[5].Enabled = wrapper.CanPaste();
                ((GUIController)editor.GUIController).MenuManager.RefreshCurrentPane();
            }
        }

        void MenuManager_OnMenuClick(string menuItemID)
        {
            /*
            MessageBox.Show("You called " + menuItemID + " | " + ID_MENU_EDIT_UNDO);
            if (menuItemID == ID_MENU_EDIT_UNDO)
            {
                MessageBox.Show("You called undo");
                ((NotesPane)editor.GUIController.ActivePane.Control).GetScintillaControl().Undo();
            }
             * */
        }

        private static IList<MenuCommand> GetContextMenuListItem()
        {
            List<MenuCommand> menu;

            menu = new List<MenuCommand>();
            menu.Add(new MenuCommand(ID_MENU_CONTEXT_RENAME, "&Rename..."));
            menu.Add(new MenuCommand(ID_MENU_CONTEXT_DELETE, "&Delete"));

            return (menu);
        }

        private static IList<MenuCommand> GetContextMenuListDefault()
        {
            IList<MenuCommand> menu;

            menu = new List<MenuCommand>();
            menu.Add(new MenuCommand(ID_MENU_CONTEXT_NEW_NOTE, "&New Note"));

            return (menu);
        }

        private int GetPaneIndex(NotesPane target)
        {
            int index;

            index = title.Count;
            while (index-- > 0)
                if (pane[index] == target)
                    break;

            return (index);
        }

        private void RemoveItem(int index)
        {
            editor.GUIController.RemovePaneIfExists(document[index]);
            title.RemoveAt(index);
            document.RemoveAt(index);
            pane.RemoveAt(index);
            RebuildTree();
        }

        public IList<MenuCommand> GetContextMenu(string control)
		{
			IList<MenuCommand> menu;
			int size;

			menu = new List<MenuCommand>();
			size = title.Count;
			while (size-- > 0)
			{
				if (control == QualifyID(ID__TYPE_NODE, title[size]))
				{
					selected = size;
                    menu = contextMenuListItem;
					break;
				}
			}
			if (size == -1)
                menu = contextMenuListGeneral;

			return menu;
		}

		public void OnPanelClosing(NotesPane source, bool canCancel, ref bool cancelClose)
		{
			DialogResult result;
			int size;

            size = GetPaneIndex(source);
			if (size > -1 && canCancel && saved[size] != pane[size].GetEditor().Text)
			{
				result = MessageBox.Show("Do you want to save changes to \"" + title[size] + "\"?", "Save Changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
				if (result == DialogResult.Yes)
					SaveItem(size);
				else
					if (result == DialogResult.Cancel)
						cancelClose = true;
			}
		}

        public void OnTextChanged(NotesPane source)
        {
            int index;

            index = GetPaneIndex(source);
            if (index > -1)
                if (source.GetEditor().Text != saved[index])
                    source.DockingContainer.Text = title[index] + "*";
                else
                    source.DockingContainer.Text = title[index];
            AbleExtraMenu(source);
        }

        public void OnWindowActivated(NotesPane source)
        {
            AbleExtraMenu(source);
        }

        private void OpenItem(int index)
		{
			NotesPane note;
			ContentDocument page;
			IScriptEditorControl script;
			bool loaded;
			String filename;

			filename = title[index];
            if (document[index] == null)
			{
				note = new NotesPane(editor, this);
				page = new ContentDocument(note, filename, this, ID_ICON_LEAF);
                // Uncomment this for additional menu next to File
                // TODO: Event bindings
                //page.MainMenu = GetExtraMenu();
                //MethodInfo x = ((GUIController)editor.GUIController).GetType().GetMethod("RegisterMenuCommand", BindingFlags.NonPublic);
                //MessageBox.Show("x: {" + x + "}");
                //((GUIController)editor.GUIController).GetType().GetMethod("RegisterMenuCommand", BindingFlags.NonPublic).Invoke((GUIController)editor.GUIController, new object[] { extraMenu.Commands[0].ID, this });
                editor.GUIController.AddOrShowPane(page);
				pane[index] = note;
				document[index] = page;
                loaded = false;
			}
			else
			{
				page = document[index];
				if (page.Visible)
					loaded = true;
				else
					loaded = false;
				editor.GUIController.AddOrShowPane(page);
				note = pane[index];
			}
			if (!loaded)
			{
				script = note.GetEditor();
				note.DockingContainer.Text = filename;
                if (File.Exists(filename))
                {
                    try
                    {
                        saved[index] = File.ReadAllText(filename);
                        script.Text = saved[index];
                        script.Control.Focus();
                    }
                    catch (Exception)
                    {
                        editor.GUIController.RemovePaneIfExists(page);
                        editor.GUIController.ShowMessage("An error occurred while reading \"" + filename + "\".", MessageBoxIconType.Error);
                    }
                }
			}
		}

		void IEditorComponent.CommandClick(string control)
		{
			DialogResult result;
			ContentDocument page;
			int size;
			String filename;
			String leaf;

            size = title.Count;
			while (size-- > 0)
			{
				if (control == QualifyID(ID__TYPE_NODE, title[size]))
				{
					OpenItem(size);
					break;
				}
			}
			if (size == -1)
			{
				if (control == ID_MENU_CONTEXT_NEW_NOTE)
				{
					size = 1;
					filename = "New Note.txt";
					while (title.IndexOf(filename) > -1)
					{
						size++;
						filename = "New Note " + size + ".txt";
					}
					title.Add(filename);
					document.Add(null);
					pane.Add(null);
					saved.Add("");
					editor.GUIController.ProjectTree.StartFromNode(this, ID_NODE_ROOT);
					leaf = QualifyID(ID__TYPE_NODE, filename);
					IProjectTreeItem item = editor.GUIController.ProjectTree.AddTreeLeaf(this, leaf, filename, ID_ICON_LEAF, false);
					editor.GUIController.ProjectTree.SelectNode(this, leaf);
					if (!File.Exists(filename))
						try
						{
							File.WriteAllText(filename, "");
						}
						catch (Exception)
						{
						}
				}
				else
					if (control == ID_MENU_CONTEXT_DELETE)
					{
						filename = title[selected];
						if (MessageBox.Show("Are you sure you want to delete \"" + filename + "\"?", "Delete Note", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) == DialogResult.Yes)
						{
                            if (File.Exists(filename))
                            {
                                try
                                {
                                    File.Delete(filename);
                                    RemoveItem(selected);
                                }
                                catch (Exception)
                                {
                                    MessageBox.Show("An error occurred while deleting \"" + filename + "\".");
                                }
                            }
                            else
                                RemoveItem(selected);
						}
					}
					else
						if (control == ID_MENU_CONTEXT_RENAME)
						{
							filename = title[selected];
							leaf = filename;
							do
							{
								result = InputBox("Rename Note", "Filename:", ref leaf);
								if (result == DialogResult.OK)
								{
									size = leaf.IndexOfAny(Path.GetInvalidFileNameChars());
									if (size > -1)
										editor.GUIController.ShowMessage("A filename cannot contain the character \"" + leaf.Substring(size, 1) + "\".", MessageBoxIconType.Warning);
								}
								else
									size = -1;
							}
							while(size > -1);
							if (result == DialogResult.OK && leaf != null)
							{
								if (!File.Exists(leaf))
								{
									try
									{
										if(File.Exists(filename))
											File.Move(filename, leaf);
										page = document[selected];
										if (page != null && page.Visible)
											page.Name = leaf;
										title[selected] = leaf;
										RebuildTree();
										editor.GUIController.ProjectTree.SelectNode(this, QualifyID(ID__TYPE_NODE, leaf));
									}
									catch (Exception)
									{
										editor.GUIController.ShowMessage("An error occurred while renaming \"" + filename + "\".", MessageBoxIconType.Error);
									}
								}
								else
									editor.GUIController.ShowMessage("A file with the name \"" + leaf + "\" already exists.", MessageBoxIconType.Error);
							}
						}
			}
		}

		private void RebuildTree()
		{
			String filename;
			int index;
			int size;

			size = title.Count;
			editor.GUIController.ProjectTree.StartFromNode(this, ID_NODE_ROOT);
			editor.GUIController.ProjectTree.RemoveAllChildNodes(this, ID_NODE_ROOT);
			for (index = 0; index < size; index++)
			{
				filename = title[index];
				editor.GUIController.ProjectTree.AddTreeLeaf(this, QualifyID(ID__TYPE_NODE, filename), filename, ID_ICON_LEAF, false);
			}
		}

		void IEditorComponent.PropertyChanged(string propertyName, object oldValue)
		{
		}

		private void SaveItem(int index)
		{
			ContentDocument page;
			String filename;
			String text;

			filename = title[index];
			page = document[index];
			text = pane[index].GetEditor().Text;
			try
			{
				File.WriteAllText(filename, text);
				saved[index] = text;
                pane[index].DockingContainer.Text = filename;
			}
			catch (Exception)
			{
				editor.GUIController.ShowMessage("An error occurred while writing \"" + filename + "\".", MessageBoxIconType.Error);
			}
		}

		void IEditorComponent.BeforeSaveGame()
		{
			ContentDocument page;
			int size;

			size = title.Count;
			while (size-- > 0)
			{
				page = document[size];
				if (page != null && page.Visible)
					SaveItem(size);
			}
		}

		void IEditorComponent.RefreshDataFromGame()
		{
			ContentDocument page;
			int size;

			size = title.Count;
			while(size-- > 0)
			{
				page = document[size];
				if (page != null)
					editor.GUIController.RemovePaneIfExists(page);
			}
		}

		void IEditorComponent.GameSettingsChanged()
		{
		}

		void IEditorComponent.ToXml(System.Xml.XmlTextWriter writer)
		{
			writer.WriteElementString("NoteFileList", string.Join("|", title.ToArray()));
		}

		void IEditorComponent.FromXml(System.Xml.XmlNode node)
		{
			string filename;
            string delimited;
			int size;
			int index;

            editor.GUIController.ProjectTree.StartFromNode(this, ID_NODE_ROOT);
            editor.GUIController.ProjectTree.RemoveAllChildNodes(this, ID_NODE_ROOT);
            if (node != null)
			{
                delimited = node.SelectSingleNode("NoteFileList").InnerText;
                if (delimited.Trim().Length > 0)
                {
    				title = new List<string>(delimited.Split('|'));
    			    size = title.Count;
                }
                else
                {
    				title = new List<string>();
                    size = 0;
                }
				pane = new List<NotesPane>(size);
				document = new List<ContentDocument>(size);
				saved = new List<String>(size);
			    for(index = 0; index < size; index++)
			    {
				    filename = title[index];
				    editor.GUIController.ProjectTree.AddTreeLeaf(this, QualifyID(ID__TYPE_NODE, filename), filename, ID_ICON_LEAF, false);
				    pane.Add(null);
				    document.Add(null);
				    saved.Add("");
			    }
			}
			else
			{
				title = new List<string>();
				pane = new List<NotesPane>();
				document = new List<ContentDocument>();
                saved = new List<String>();
            }
		}

		void IEditorComponent.EditorShutdown()
		{
		}

		public static DialogResult InputBox(string title, string promptText, ref string value)
		{
			Form form = new Form();
			Label label = new Label();
			TextBox textBox = new TextBox();
			Button buttonOk = new Button();
			Button buttonCancel = new Button();

			form.Text = title;
			label.Text = promptText;
			textBox.Text = value;

			buttonOk.Text = "OK";
			buttonCancel.Text = "Cancel";
			buttonOk.DialogResult = DialogResult.OK;
			buttonCancel.DialogResult = DialogResult.Cancel;

			label.SetBounds(9, 20, 372, 13);
			textBox.SetBounds(12, 36, 372, 20);
			buttonOk.SetBounds(228, 72, 75, 23);
			buttonCancel.SetBounds(309, 72, 75, 23);

			label.AutoSize = true;
			textBox.Anchor = textBox.Anchor | AnchorStyles.Right;
			buttonOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
			buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

			form.ClientSize = new Size(396, 107);
			form.Controls.AddRange(new Control[] { label, textBox, buttonOk, buttonCancel });
			form.ClientSize = new Size(Math.Max(300, label.Right + 10), form.ClientSize.Height);
			form.FormBorderStyle = FormBorderStyle.FixedDialog;
			form.StartPosition = FormStartPosition.CenterScreen;
			form.MinimizeBox = false;
			form.MaximizeBox = false;
			form.AcceptButton = buttonOk;
			form.CancelButton = buttonCancel;

			DialogResult dialogResult = form.ShowDialog();
			value = textBox.Text;
			return dialogResult;
		}
	}
}
