using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
	/// Interaction logic for AnimatedImage.xaml
	/// </summary>
	public partial class AnimatedImage : UserControl, INotifyPropertyChanged
	{
		//-----------------------------------------------------------------------
		public IEnumerable<BitmapImage> Frames
		{
			get { return (IEnumerable<BitmapImage>)GetValue(FramesProperty); }
			set { SetValue(FramesProperty, value); }
		}
		private List<BitmapImage> m_frames;

		//-----------------------------------------------------------------------
		public static readonly DependencyProperty FramesProperty =
			DependencyProperty.Register("Frames", typeof(IEnumerable<BitmapImage>), typeof(AnimatedImage), new PropertyMetadata(new List<BitmapImage>(), (s, a) =>
			{
				var i = (AnimatedImage)s;
				i.m_frames = new List<BitmapImage>();
				
				if (a.NewValue != null)
				{
					var newItems = (IEnumerable<BitmapImage>)a.NewValue;
					i.m_frames.AddRange(newItems);
				}

				if (i.m_frames.Count == 0)
				{
					i.m_frames = null;
				}
			}));

		//-----------------------------------------------------------------------
		public int UpdateRate
		{
			get { return (int)GetValue(UpdateRateProperty); }
			set { SetValue(UpdateRateProperty, value); }
		}

		//-----------------------------------------------------------------------
		public static readonly DependencyProperty UpdateRateProperty =
			DependencyProperty.Register("UpdateRate", typeof(int), typeof(AnimatedImage), new PropertyMetadata(500, (s, a) =>
			{

			}));

		//-----------------------------------------------------------------------
		public BitmapImage CurrentFrame
		{
			get { return m_frames?[frame]; }
		}

		//-----------------------------------------------------------------------
		public AnimatedImage()
		{
			//DataContext = this;

			InitializeComponent();

			Loaded += (obj, args) =>
			{
				lastTime = DateTime.Now;
				CompositionTarget.Rendering += Update;
			};

			Unloaded += (obj, args) =>
			{
				CompositionTarget.Rendering -= Update;
			};
		}

		//-----------------------------------------------------------------------
		public void Update(object sender, EventArgs args)
		{
			var current = DateTime.Now;
			var diff = current - lastTime;
			var elapsedMS = (int)diff.TotalMilliseconds;
			lastTime = current;

			if (m_frames == null) return;

			accumulator += elapsedMS;
			
			while (accumulator > UpdateRate)
			{
				accumulator -= UpdateRate;
				frame++;

				if (frame >= m_frames.Count)
				{
					frame = 0;
				}
			}

			RaisePropertyChangedEvent("CurrentFrame");
		}

		//-----------------------------------------------------------------------
		private DateTime lastTime = DateTime.Now;
		private int frame = 0;
		private int accumulator = 0;

		//--------------------------------------------------------------------------
		public event PropertyChangedEventHandler PropertyChanged;

		//-----------------------------------------------------------------------
		public void RaisePropertyChangedEvent
		(
			[CallerMemberName] string i_propertyName = ""
		)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(i_propertyName));
			}
		}
	}
}
