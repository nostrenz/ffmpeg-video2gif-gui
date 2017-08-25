using System;
using System.Windows;
using System.Diagnostics;
using Video2Gif.Properties;
using File = System.IO.File;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace Video2Gif
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		const string PALETTE = "palette.png";

		private string filters = null;
		private string firstPart = null;
		private string paletteUse = null;
		private string output = null;

		public MainWindow()
		{
			InitializeComponent();

			this.TextBox_FFmepg.Text = Settings.Default.FFmpeg;

			this.TextBox_Fps.Text = Settings.Default.Fps;
			this.TextBox_StartTime.Text = Settings.Default.StartTime;
			this.TextBox_Duration.Text = Settings.Default.Duration;
			this.TextBox_Input.Text = Settings.Default.Input;
			this.TextBox_Output.Text = Settings.Default.Output;
		}

		/*
		============================================
		Private
		============================================
		*/

		#region Private

		/// <summary>
		/// Create and start a new FFmpeg process.
		/// </summary>
		/// <param name="arguments"></param>
		/// <param name="eventHandler"></param>
		private void StartFfmpeg(string arguments, EventHandler eventHandler)
		{
			Process process = new Process();

			process.StartInfo.FileName = Settings.Default.FFmpeg;
			process.StartInfo.Arguments = arguments;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.ErrorDialog = true;
			process.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
			process.StartInfo.RedirectStandardOutput = false;
			process.EnableRaisingEvents = true;
			process.Exited += eventHandler;

			process.Start();
		}

		/// <summary>
		/// Call FFmpeg to create a palette from the source video.
		/// </summary>
		/// <param name="startTime"></param>
		/// <param name="duration"></param>
		/// <param name="input"></param>
		/// <param name="filters"></param>
		private void CreatePalette(string startTime, string duration, string input, string filters)
		{
			string statsMode = this.ComboBox_StatsMode.SelectedValue.ToString();
			string paletteGen = "palettegen=stats_mode=" + statsMode;

			this.firstPart = String.Format(@"-ss {0} -t {1} -i ""{2}"" ", startTime, duration, input);

			string arguments = String.Format(
				this.firstPart + @"-vf ""{0},{1}"" -y ""{2}""",
				filters, paletteGen, this.PalettePath
			);

			this.AddpendLogBox(Settings.Default.FFmpeg + " " + arguments);
			this.StartFfmpeg(arguments, new EventHandler(this.Palette_Created));
		}

		/// <summary>
		/// Call FFmpeg to create a gif from the source video.
		/// A palette needs to be created first using CreatePalette().
		/// </summary>
		/// <param name="filters"></param>
		private void CreateGif(string filters)
		{
			string arguments = String.Format(
				firstPart + @"-i ""{0}"" -lavfi ""{1}[x]; [x] [1:v] {2}"" -y ""{3}""",
				this.PalettePath, filters, this.paletteUse, this.output
			);

			this.AddpendLogBox(Settings.Default.FFmpeg + " " + arguments);
			this.StartFfmpeg(arguments, new EventHandler(this.Gif_Created));
		}

		/// <summary>
		/// Open a file selection dialog and return the result.
		/// </summary>
		/// <param name="filter"></param>
		/// <returns></returns>
		private string SelectFile(string filter)
		{
			OpenFileDialog dlg = new OpenFileDialog();
			dlg.Filter = filter;
			dlg.Multiselect = false;

			if (!(bool)dlg.ShowDialog()) {
				return null;
			}

			return dlg.FileName;
		}

		/// <summary>
		/// Append a message into the log textbox.
		/// </summary>
		/// <param name="msg"></param>
		private void AddpendLogBox(string msg)
		{
			Dispatcher.BeginInvoke(new Action(delegate {
				this.TextBlock_Logs.Text += msg + "\n\n";
			}), System.Windows.Threading.DispatcherPriority.ApplicationIdle, null);
		}

		#endregion Private

		/*
		============================================
		Accessor
		============================================
		*/

		#region Accessor

		private string PalettePath
		{
			get { return App.appDir + @"\" + PALETTE; }
		}

		#endregion Accessor

		/*
		============================================
		Event
		============================================
		*/

		#region Event

		/// <summary>
		/// Called by clicking on the "Convert" button, start the conversion from video to a gif.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Convert_Click(object sender, RoutedEventArgs e)
		{
			string ffmpeg = this.TextBox_FFmepg.Text;

			if (!File.Exists(ffmpeg)) {
				MessageBox.Show("Bad FFmpeg path.");

				return;
			}

			// Save FFmpeg path
			Settings.Default.FFmpeg = ffmpeg;
			Settings.Default.Save();

			string input = this.TextBox_Input.Text;

			if (!File.Exists(input)) {
				MessageBox.Show("Input file does not exists.");

				return;
			}

			string startTime = TextBox_StartTime.Text;
			string duration = TextBox_Duration.Text;
			this.output = this.TextBox_Output.Text;

			// More filters here: https://ffmpeg.org/ffmpeg-all.html#toc-Video-Filters
			this.filters = "fps=" + this.TextBox_Fps.Text + ",scale=" + this.TextBox_Width.Text + ":-1:flags=lanczos";

			// "bayer:bayer_scale=$USE_BAYERSCALE", "floyd_steinberg", "sierra2", "sierra2_4a" (default), "none", "heckbert" (not recommended)
			string dither = this.ComboBox_Dither.SelectedValue.ToString();

			// "rectangle" (better when only a part of the image is moving) or "none" (default)
			string diffMode = (bool)this.RadioButton_DiffMode_Rectangle.IsChecked ? "rectangle" : "none";

			if (dither == "bayer") {
				// Only used when dither bayer, a number between 1 and 5, 1 more appearant and 5 is better quality
				string bayerScale = this.ComboBox_BayerScale.SelectedValue.ToString();

				dither += ":bayer_scale=" + bayerScale;
			}

			this.paletteUse = String.Format(
				"paletteuse=dither={0}:diff_mode={1}:new={2}",
				dither, diffMode, (bool)this.CheckBox_New.IsChecked ? "1" : "0"
			);

			this.CreatePalette(startTime, duration, input, filters);

			this.TextBlock_Logs.Text = "";
			this.Button_Convert.IsEnabled = false;
			this.ProgressBar.IsIndeterminate = true;
		}

		/// <summary>
		/// Called once the palette is created.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Palette_Created(object sender, EventArgs e)
		{
			if (!File.Exists(this.PalettePath)) {
				MessageBox.Show("Cannot create palette.");

				return;
			}

			this.CreateGif(this.filters);
		}

		/// <summary>
		/// Called once the gif is created.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Gif_Created(object sender, EventArgs e)
		{
			File.Delete(this.PalettePath);

			Dispatcher.BeginInvoke(new Action(delegate {
				this.ProgressBar.IsIndeterminate = false;
				this.Button_Convert.IsEnabled = true;

				Settings.Default.Fps = this.TextBox_Fps.Text;
				Settings.Default.StartTime = this.TextBox_StartTime.Text;
				Settings.Default.Duration = TextBox_Duration.Text;
				Settings.Default.Input = this.TextBox_Input.Text;
				Settings.Default.Output = this.TextBox_Output.Text;

				Settings.Default.Save();
			}), System.Windows.Threading.DispatcherPriority.ApplicationIdle, null);
		}

		/// <summary>
		/// Called when clicking the FFMPEG browser button.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Browse_Ffmpeg_Click(object sender, RoutedEventArgs e)
		{
			string file = this.SelectFile("ffmpeg.exe|*.exe");

			if (file != null) {
				this.TextBox_FFmepg.Text = file;
			}
		}

		/// <summary>
		/// Called when clicking the input browser button.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Browse_Input_Click(object sender, RoutedEventArgs e)
		{
			string file = this.SelectFile("Video file|*.mp4;*.mkv;*.avi;*.mov");

			if (file != null) {
				this.TextBox_Input.Text = file;

				string dirName = System.IO.Directory.GetParent(file).FullName;

				this.output = dirName + @"\out.gif";
				this.TextBox_Output.Text = this.output;
			}
		}

		/// <summary>
		/// Called when selecting another item into the Dither combobox, enable or disable the SaberScale combobox depending on the selected item.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ComboBox_Dither_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			this.ComboBox_BayerScale.IsEnabled = (this.ComboBox_Dither.SelectedValue.ToString() == "bayer");
		}

		#endregion Event
	}
}
