using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace StructuredXmlEditor.View
{
	/// <summary>
	/// Interaction logic for SkeletalAnimationEditor.xaml
	/// </summary>
	public partial class SkeletalAnimationEditor : UserControl
	{
		public SkeletalEditorPanel EditorPanel { get; set; }

		public SkeletalAnimationEditor()
		{
			EditorPanel = new SkeletalEditorPanel();

			InitializeComponent();
		}
	}
}
