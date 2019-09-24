using AGS.Types;

namespace AGS.Plugin.Notes
{
	[RequiredAGSVersion("3.3.0.0")]
	public class NotesPlugin : AGS.Types.IAGSEditorPlugin
	{
		public NotesPlugin(IAGSEditor editor)
		{
			editor.AddComponent(new NotesComponent(editor));
		}

		public void Dispose()
		{
		}
	}
}
