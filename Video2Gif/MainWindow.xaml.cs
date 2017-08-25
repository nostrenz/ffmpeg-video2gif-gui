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
		private void CreatePalette()
		{
			this.paletteUse = String.Format(
				"paletteuse=dither={0}:diff_mode={1}:new={2}",
				this.DitherWithScale, this.DiffMode, this.New
			);

			string arguments = String.Format(
				this.firstPart + @"-vf ""{0},palettegen=stats_mode={1}"" -y ""{2}""",
				this.filters, this.StatsMode, this.PalettePath
			);

			this.AddpendLogBox(Settings.Default.FFmpeg + " " + arguments);
			this.StartFfmpeg(arguments, new EventHandler(this.Palette_Created));
		}

		/// <summary>
		/// Call FFmpeg to create a gif from the source video.
		/// A palette needs to be created first using CreatePalette().
		/// </summary>
		/// <param name="paletteUse"></param>
		private void CreateGif(string paletteUse=null)
		{
			string arguments = null;

			if (paletteUse != null) {
				arguments = String.Format(
					firstPart + @"-i ""{0}"" -lavfi ""{1}[x]; [x] [1:v] {2}"" -y ""{3}""",
					this.PalettePath, this.filters, paletteUse, this.output
				);
			} else {
				arguments = String.Format(
					firstPart + @"-lavfi ""{0}"" -y ""{1}""",
					this.filters, this.output
				);
			}

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

		private string Ffmpeg
		{
			get { return this.TextBox_FFmepg.Text; }
		}

		private string PalettePath
		{
			get { return App.appDir + @"\" + PALETTE; }
		}

		private string CurvePreset
		{
			get { return this.ComboBox_CurvePreset.SelectedValue.ToString(); }
		}

		private string ResizeType
		{
			get { return this.ComboBox_ResizeType.SelectedValue.ToString(); }
		}

		private string Fps
		{
			get { return this.TextBox_Fps.Text; }
		}

		private string Input
		{
			get { return this.TextBox_Input.Text; }
		}

		private string Output
		{
			get { return this.TextBox_Output.Text; }
		}

		private string SizeWidth
		{
			get { return this.TextBox_Width.Text; }
		}

		private string DiffMode
		{
			get { return (bool)this.RadioButton_DiffMode_Rectangle.IsChecked ? "rectangle" : "none"; }
		}

		private string BayerScale
		{
			get { return this.ComboBox_BayerScale.SelectedValue.ToString(); }
		}

		private string Dither
		{
			get { return this.ComboBox_Dither.SelectedValue.ToString(); }
		}

		private string DitherWithScale
		{
			get
			{
				string dither = this.Dither;

				// The bayer dither needs a scale option
				if (dither == "bayer") {
					dither += ":bayer_scale=" + this.BayerScale;
				}

				return dither;
			}
		}

		private string New
		{
			get { return (bool)this.CheckBox_New.IsChecked ? "1" : "0"; }
		}

		private string StatsMode
		{
			get { return this.ComboBox_StatsMode.SelectedValue.ToString(); }
		}

		private string StartTime
		{
			get { return this.TextBox_StartTime.Text; }
		}

		private string Duration
		{
			get { return this.TextBox_Duration.Text; }
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
			string ffmpeg = this.Ffmpeg;
			this.output = this.Output;

			if (!File.Exists(ffmpeg)) {
				MessageBox.Show("Bad FFmpeg path.");

				return;
			}

			// Save FFmpeg path
			Settings.Default.FFmpeg = ffmpeg;
			Settings.Default.Save();

			string input = this.Input;

			if (!File.Exists(input)) {
				MessageBox.Show("Input file does not exists.");

				return;
			}

			this.firstPart = String.Format(
				@"-ss {0} -t {1} -i ""{2}"" ",
				this.StartTime, this.Duration, input
			);

			// More filters here: https://ffmpeg.org/ffmpeg-filters.html#Video-Filters
			this.filters = String.Format(
				"curves={0},fps={1},scale={2}:-1:flags={3}",
				this.CurvePreset, this.Fps, this.SizeWidth, this.ResizeType
			);

			// Use a palette
			if ((bool)this.CheckBox_UsePalette.IsChecked) {
				this.CreatePalette();
			} else { // Do not use a palette
				this.CreateGif();
			}

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

			this.CreateGif(this.paletteUse);
		}

		/// <summary>
		/// Called once the gif is created.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Gif_Created(object sender, EventArgs e)
		{
			string palettePath = this.PalettePath;

			if (File.Exists(palettePath)) {
				File.Delete(palettePath);
			}

			Dispatcher.BeginInvoke(new Action(delegate {
				this.ProgressBar.IsIndeterminate = false;
				this.Button_Convert.IsEnabled = true;

				Settings.Default.Fps = this.Fps;
				Settings.Default.StartTime = this.StartTime;
				Settings.Default.Duration = this.Duration;
				Settings.Default.Input = this.Input;
				Settings.Default.Output = this.Output;

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

				this.TextBox_Output.Text = dirName + @"\out.gif";
			}
		}

		/// <summary>
		/// Called when selecting another item into the Dither combobox, enable or disable the SaberScale combobox depending on the selected item.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ComboBox_Dither_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			this.ComboBox_BayerScale.IsEnabled = (this.Dither == "bayer");
		}

		/// <summary>
		/// Called when clicking the UsePalette checkbox.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void CheckBox_UsePalette_Clickl(object sender, RoutedEventArgs e)
		{
			bool usePalette = (bool)this.CheckBox_UsePalette.IsChecked;

			this.ComboBox_StatsMode.IsEnabled = usePalette;
			this.ComboBox_Dither.IsEnabled = usePalette;
			this.ComboBox_Dither.IsEnabled = usePalette;
			this.RadioButton_DiffMode_None.IsEnabled = usePalette;
			this.RadioButton_DiffMode_Rectangle.IsEnabled = usePalette;
			this.CheckBox_New.IsEnabled = usePalette;
		}

		#endregion Event
	}
}
