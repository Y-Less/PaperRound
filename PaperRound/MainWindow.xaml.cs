/*\
 * The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * https://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * The Original Code is the "PaperRound" wallpaper tiler.
 * 
 * The Initial Developer of the Original Code is Alex "Y_Less" Cole.
 * All Rights Reserved.
\*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using F = System.Windows.Forms;
using System.ComponentModel;
using Microsoft.Win32;
using System.Timers;
using D = System.Drawing;
using System.Runtime.Serialization;
using System.IO;
using System.Xml.Serialization;

using System.Linq;

namespace MultiWallpaper
{
	[Serializable]
	public class Wallpaper : IXmlSerializable, INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		public Wallpaper()
		{
			X_ = 0;
			Y_ = 0;
			H_ = 0;
			originalHeight = 0;
			Src_ = null;
			Scale_ = 1.0;
			ShowExtras = false;
		}

		public System.Xml.Schema.XmlSchema GetSchema()
		{
			return null;
		}

		public void ReadXml(System.Xml.XmlReader reader)
		{
			reader.MoveToContent();
			Src = reader.GetAttribute("Source");
			X = Double.Parse(reader.GetAttribute("X"));
			Y = Double.Parse(reader.GetAttribute("Y"));
			H = Double.Parse(reader.GetAttribute("H"));
		}

		public void WriteXml(System.Xml.XmlWriter writer)
		{
			writer.WriteAttributeString("Source", Src);
			writer.WriteAttributeString("X", X.ToString());
			writer.WriteAttributeString("Y", Y.ToString());
			writer.WriteAttributeString("H", H.ToString());
		}

		private double X_;
		private double Y_;
		private double H_;
		[XmlIgnore]
		public double originalHeight { get; private set; }
		private double Scale_;
		private string Src_;

		public double Scale { get { return Scale_; } set { Scale_ = value; Notify("Width"); Notify("Width"); Notify("Margin"); } }

		public double X { get { return X_; } set { X_ = value; Notify("Margin"); } }
		public double Y { get { return Y_; } set { Y_ = value; Notify("Margin"); } }
		public double W { get { return H_ * Ratio; } }
		public double H { get { return H_; } set { H_ = value; Notify("Width"); Notify("Height"); } }

		[XmlIgnore]
		public Visibility ExtrasEnabled { get; private set; }

		[XmlIgnore]
		public bool ShowExtras
		{
			get
			{
				return ExtrasEnabled == Visibility.Visible;
			}

			set
			{
				if (value)
					ExtrasEnabled = Visibility.Visible;
				else
					ExtrasEnabled = Visibility.Collapsed;
				Notify("ExtrasEnabled");
			}
		}

		[XmlIgnore]
		public double Ratio { get; private set; }

		public Thickness Margin
		{
			get
			{
				return new Thickness(X_ / Scale_, Y_ / Scale_, 0.0, 0.0);
			}
		}

		public double Width
		{
			get
			{
				return H_ * Ratio / Scale_;
			}
		}

		public double Height
		{
			get
			{
				return H_ / Scale_;
			}
		}

		public double Zoom
		{
			get
			{
				return H_ / originalHeight;
			}
		}

		public string Src
		{
			get
			{
				return Src_;
			}
			
			set
			{
				if (value == "")
					Src_ = null;
				else
					Src_ = value;
				if (Src_ != null)
				{
					System.Drawing.Image
						image = System.Drawing.Image.FromFile(Src_);
					originalHeight = H_ = image.Height;
					X_ = 0;
					Y_ = 0;
					Ratio = image.Width / H_;
				}
				Notify("Src");
				Notify("Width");
				Notify("Height");
				Notify("Margin");
			}
		}

		private void Notify(String info)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(info));
			}
		}
	}

	public class ScreenWallpaper
	{
		public string Device;
		public Wallpaper Wallpaper;
	}

	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private int
			minX_ = 0,
			maxX_ = 0,
			minY_ = 0,
			maxY_ = 0,
			width_ = 0,
			height_ = 0;
		
		private double
			scale_ = 0.0;

		private Dictionary<F.Screen, Wallpaper>
			wallpapers_;
		
		private Timer
			zoomTimer_ = new Timer { Enabled = false, AutoReset = false };

		private bool
			repeating_ = false;

		private Button
			clickButton_ = null;

		private int
			zoomBy_ = 0;

		private Button
			dragging_ = null;

		private Point
			dragXY_,
			paperXY_;

		private string
			filename_;

		public MainWindow()
		{
			InitializeComponent();
			InitialiseScreens();
			NewImage();
			zoomTimer_.Elapsed += (object sender, ElapsedEventArgs e) =>
			{
				this.Dispatcher.BeginInvoke((Action)(() => RepeatZoom()));
			};
		}

		public void NewImage()
		{
			wallpapers_ = new Dictionary<F.Screen, Wallpaper>();
			foreach (var screen in F.Screen.AllScreens)
			{
				wallpapers_[screen] = new Wallpaper();
			}
			filename_ = "";
		}

		private double GetScale(double x1, double y1, int x2, int y2)
		{
			// Work out a scaling factor so that x2,y2 will fit nicely in x1,y1.
			double
				width = x1 * 0.90,
				height = y1 * 0.90;
			return Math.Max(x2 / width, y2 / height);
		}

		private void InitialiseScreens()
		{
			//InitialiseScreens.
			foreach (var screen in F.Screen.AllScreens)
			{
				minX_ = Math.Min(minX_, screen.Bounds.Left);
				maxX_ = Math.Max(maxX_, screen.Bounds.Right);
				minY_ = Math.Min(minY_, screen.Bounds.Top);
				maxY_ = Math.Max(maxY_, screen.Bounds.Bottom);
			}
			width_ = maxX_ - minX_;
			height_ = maxY_ - minY_;
		}

		private void RenderScreen(F.Screen screen, double scale, double offX, double offY)
		{
			int
				x = screen.Bounds.Left - minX_,
				y = screen.Bounds.Top - minY_,
				width = screen.Bounds.Width,
				height = screen.Bounds.Height;
			double
				xD = x / scale + offX,
				yD = y / scale + offY,
				wD = width / scale,
				hD = height / scale;
			Button
				b = new Button();
			b.Style = this.FindResource("ScreenControl") as Style;
			b.Width = wD;
			b.Height = hD;
			wallpapers_[screen].Scale = scale;
			b.Content = wallpapers_[screen];
			Canvas.SetLeft(b, xD);
			Canvas.SetTop(b, yD);
			Canvas_Main.Children.Add(b);
		}

		private void RenderAllScreens()
		{
			Canvas_Main.Children.Clear();
			double
				offX = Canvas_Main.ActualWidth,
				offY = Canvas_Main.ActualHeight;
			scale_ = GetScale(offX, offY, width_, height_);
			foreach (var screen in F.Screen.AllScreens)
			{
				RenderScreen(screen, scale_, offX * 0.05, offY * 0.05);
			}
		}

		private void RepeatZoom()
		{
			(clickButton_.Content as Wallpaper).H += zoomBy_;
			repeating_ = true;
			zoomTimer_.Stop();
			zoomTimer_.Interval = 50;
			zoomTimer_.Start();
		}

		private void StartZooming(Button button, int z)
		{
			clickButton_ = button as Button;
			if ((clickButton_.Content as Wallpaper).Src == null)
			{
				clickButton_ = null;
			}
			else
			{
				repeating_ = false;
				zoomBy_ = z;
				zoomTimer_.Stop();
				zoomTimer_.Interval = 250;
				zoomTimer_.Start();
			}
		}

		private void OnMouseUp(object sender, RoutedEventArgs e)
		{
			if (clickButton_ != null && clickButton_ == sender && !repeating_)
			{
				// Just a normal click, not a press-and-hold.
				(clickButton_.Content as Wallpaper).H += zoomBy_;
				e.Handled = true;
			}
			zoomTimer_.Stop();
			clickButton_ = null;
			if (dragging_ != null)
			{
				dragging_ = null;
				e.Handled = true;
			}
		}

		private void OnZoomInIn(object sender, RoutedEventArgs e)
		{
			StartZooming(sender as Button, 10);
			e.Handled = true;
		}

		private void OnZoomIn(object sender, RoutedEventArgs e)
		{
			StartZooming(sender as Button, 1);
			e.Handled = true;
		}

		private void OnZoomOut(object sender, RoutedEventArgs e)
		{
			StartZooming(sender as Button, -1);
			e.Handled = true;
		}

		private void OnZoomOutOut(object sender, RoutedEventArgs e)
		{
			StartZooming(sender as Button, -10);
			e.Handled = true;
		}

		private void OnSelect(object sender, EventArgs e)
		{
			OpenFileDialog
				fileDialog = new OpenFileDialog();
			fileDialog.Filter = "Image Files|*.bmp;*.png;*.gif;*.jpg;*.jpeg|All Files|*.*";
			fileDialog.Title = "Select Image";
			((sender as Button).Content as Wallpaper).Src = (fileDialog.ShowDialog() == true) ? fileDialog.FileName : null;
		}

		private void OnWindow_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			RenderAllScreens();
		}

		private void OnStartDrag(object sender, RoutedEventArgs e)
		{
			dragging_ = (sender as Image).TemplatedParent as Button;
			Wallpaper
				w = dragging_.Content as Wallpaper;
			dragXY_ = Mouse.GetPosition(dragging_);
			paperXY_ = new Point(w.X, w.Y);
			e.Handled = true;
		}

		private void OnMouseOffExtras(object sender, MouseEventArgs e)
		{
			//MessageBox.Show("gone");
			foreach (var w in wallpapers_.Values)
			{
				w.ShowExtras = false;
			}
		}

		private void OnDrag(object sender, MouseEventArgs e)
		{
			if (dragging_ != null && sender as Image != null && dragging_ == (sender as Image).TemplatedParent as Button)
			{
				Wallpaper
					w = dragging_.Content as Wallpaper;
				Point
					dragXY = Mouse.GetPosition(dragging_);
				// The offset of the mouse pointer from the start position.
				double
					x = dragXY.X - dragXY_.X,
					y = dragXY.Y - dragXY_.Y;
				w.X = paperXY_.X + x * w.Scale;
				w.Y = paperXY_.Y + y * w.Scale;
				e.Handled = true;
			}
		}

		private void OnClickMore(object sender, RoutedEventArgs e)
		{
			((sender as Button).Content as Wallpaper).ShowExtras = true;
		}

		private void OnClickNew(object sender, RoutedEventArgs e)
		{
			if (MessageBox.Show("Are you sure you want to discard changes?", "New Image", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
			{
				NewImage();
				RenderAllScreens();
			}
		}

		private void OnClickSave(object sender, RoutedEventArgs e)
		{
			if (filename_ == "")
				OnClickSaveAs(sender, e);
			else
				Save(filename_);
		}

		private void OnClickSaveAs(object sender, RoutedEventArgs e)
		{
			SaveFileDialog
				fileDialog = new SaveFileDialog();
			fileDialog.FileName = "Untitled";
			fileDialog.DefaultExt = ".wallpaper";
			fileDialog.Filter = "Wallpaper Files|*.wallpaper|All Files|*.*";
			if (fileDialog.ShowDialog() == true)
			{
				filename_ = fileDialog.FileName;
				Save(filename_);
			}
		}

		private void OnClickLoad(object sender, RoutedEventArgs e)
		{
			OpenFileDialog
				fileDialog = new OpenFileDialog();
			fileDialog.Filter = "Wallpaper Files|*.wallpaper|All Files|*.*";
			fileDialog.Title = "Select Wallpaper";
			if (fileDialog.ShowDialog() == true)
				Load(fileDialog.FileName);
		}

		private void OnClickExport(object sender, RoutedEventArgs e)
		{
			SaveFileDialog
				fileDialog = new SaveFileDialog();
			fileDialog.FileName = "Wallpaper - " + DateTime.Now.ToString("yyMMddHHmmss");
			fileDialog.DefaultExt = ".png";
			fileDialog.Filter = "Image Files|*.png|All Files|*.*";
			if (fileDialog.ShowDialog() == true)
			{
				filename_ = fileDialog.FileName;
				Export(filename_);
			}
		}

		private void Save(string fname)
		{
			System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(typeof(ScreenWallpaper []));
			Stream stream = new FileStream(fname, FileMode.Create, FileAccess.Write, FileShare.None);
			//new ScreenWallpaper { 
			x.Serialize(stream, wallpapers_.Select((KeyValuePair<F.Screen, Wallpaper> kvp) =>
			{
				return new ScreenWallpaper { Device = kvp.Key.DeviceName, Wallpaper = kvp.Value };
			}).ToArray());
			stream.Close();
		}

		private void Load(string fname)
		{
			System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(typeof(ScreenWallpaper []));
			Stream stream = new FileStream(fname, FileMode.Open, FileAccess.Read, FileShare.None);
			System.Xml.XmlReader reader = System.Xml.XmlReader.Create(stream);
			ScreenWallpaper []
				wl = (ScreenWallpaper [])x.Deserialize(reader);
			stream.Close();
			foreach (var s in F.Screen.AllScreens)
			{
				wallpapers_[s] = new Wallpaper();
			}
			foreach (var k in wl)
			{
				foreach (var s in F.Screen.AllScreens)
				{
					if (s.DeviceName == k.Device)
					{
						wallpapers_[s] = k.Wallpaper;
						break;
					}
				}
			}
			RenderAllScreens();
			filename_ = fname;
		}

		private void Export(string fname)
		{
			using (D.Bitmap b = new D.Bitmap(width_, height_))
			{
				using (D.Graphics g = D.Graphics.FromImage(b))
				{
					g.Clear(D.Color.Black);
					foreach (var i in wallpapers_)
					{
						Wallpaper
							wallpaper = i.Value;
						if (wallpaper.Src == null)
							continue;
						// This maths is less simple than it may first seem - I
						// got it wrong first time as I forgot one important
						// fact.  When you tile a wallpaper, it is done relative
						// to the top-left of the primary screen, so if you have
						// monitors left-of or above the primary monitor, the
						// whole thing will mess up.  You need to make the image
						// wrap around to those extra areas.
						F.Screen
							screen = i.Key;
						double
							zoom = wallpaper.Zoom;
						int
							screenW = screen.Bounds.Width,
							screenH = screen.Bounds.Height,
							screenL = screen.Bounds.Left,
							screenT = screen.Bounds.Top;
						D.Rectangle
							srcRect = new D.Rectangle(Convert.ToInt32(0 - wallpaper.X / zoom), Convert.ToInt32(0 - wallpaper.Y / zoom), Convert.ToInt32(screenW / zoom), Convert.ToInt32(screenH / zoom));
						g.DrawImage(
							// What to draw.
							System.Drawing.Image.FromFile(wallpaper.Src),
							// Where to draw it.
							new D.Rectangle(screenL, screenT, screenW, screenH),
							// How much of it to draw
							srcRect,
							D.GraphicsUnit.Pixel);
						g.DrawImage(
							// What to draw.
							System.Drawing.Image.FromFile(wallpaper.Src),
							// Where to draw it.
							new D.Rectangle(screenL + width_, screenT, screenW, screenH),
							// How much of it to draw
							srcRect,
							D.GraphicsUnit.Pixel);
						g.DrawImage(
							// What to draw.
							System.Drawing.Image.FromFile(wallpaper.Src),
							// Where to draw it.
							new D.Rectangle(screenL, screenT + height_, screenW, screenH),
							// How much of it to draw
							srcRect,
							D.GraphicsUnit.Pixel);
						g.DrawImage(
							// What to draw.
							System.Drawing.Image.FromFile(wallpaper.Src),
							// Where to draw it.
							new D.Rectangle(screenL + width_, screenT + height_, screenW, screenH),
							// How much of it to draw
							srcRect,
							D.GraphicsUnit.Pixel);
					}
				}
				b.Save(fname, D.Imaging.ImageFormat.Png);
			}
		}

		private void OnSnapLeft(object sender, RoutedEventArgs e)
		{
			Wallpaper
				w = (sender as Button).Content as Wallpaper;
			w.X = 0;
		}

		private void OnSnapTop(object sender, RoutedEventArgs e)
		{
			Wallpaper
				w = (sender as Button).Content as Wallpaper;
			w.Y = 0;
		}

		private void OnSnapRight(object sender, RoutedEventArgs e)
		{
			Wallpaper
				w = (sender as Button).Content as Wallpaper;
			foreach (var s in wallpapers_)
			{
				if ((object)s.Value == (object)w)
				{
					w.X = s.Key.Bounds.Width - w.W;
					break;
				}
			}
		}

		private void OnSnapBottom(object sender, RoutedEventArgs e)
		{
			Wallpaper
				w = (sender as Button).Content as Wallpaper;
			foreach (var s in wallpapers_)
			{
				if ((object)s.Value == (object)w)
				{
					w.Y = s.Key.Bounds.Height - w.H;
					break;
				}
			}
		}

		private void OnSnapWidth(object sender, RoutedEventArgs e)
		{
			Wallpaper
				w = (sender as Button).Content as Wallpaper;
			foreach (var s in wallpapers_)
			{
				if ((object)s.Value == (object)w)
				{
					// We only ever modify height to preserve ratios.
					w.H = s.Key.Bounds.Width / w.Ratio;
					break;
				}
			}
		}

		private void OnSnapHeight(object sender, RoutedEventArgs e)
		{
			Wallpaper
				w = (sender as Button).Content as Wallpaper;
			foreach (var s in wallpapers_)
			{
				if ((object)s.Value == (object)w)
				{
					w.H = s.Key.Bounds.Height;
					break;
				}
			}
		}

		private void OnClickReset(object sender, RoutedEventArgs e)
		{
			Wallpaper
				w = (sender as Button).Content as Wallpaper;
			w.H = w.originalHeight;
			w.X = 0;
			w.Y = 0;
		}
	}
}

