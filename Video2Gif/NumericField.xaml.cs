using System.Windows.Controls;

namespace Video2Gif
{
	/// <summary>
	/// Interaction logic for NumericField.xaml
	/// </summary>
	public partial class NumericField : UserControl
	{
		private int number = 0;
		private int minimum = 0;
		private int maximum = 0;
		private byte maxLength = 0;

		public NumericField()
		{
			InitializeComponent();
		}

		/*
		============================================
		Private
		============================================
		*/

		#region Private

		/// <summary>
		/// Update the counter textbox with the integer value.
		/// </summary>
		private void UpdateField()
		{
			this.textbox.Text = this.number.ToString();
		}

		#endregion Private

		/*
		============================================
		Accessor
		============================================
		*/

		#region Accessor

		public string Text
		{
			get { return this.textbox.Text; }
			set { this.textbox.Text = value; }
		}

		public int Number
		{
			get { return this.number; }
			set { this.number = value; }
		}

		public int Minimum
		{
			get { return this.minimum; }
			set { this.minimum = value; }
		}

		public int Maximum
		{
			get { return this.maximum; }
			set { this.maximum = value; }
		}

		public byte MaxLength
		{
			get { return this.maxLength; }
			set { this.maxLength = value; }
		}

		#endregion Accessor

		/*
		============================================
		Event
		============================================
		*/

		#region Event

		private void TextChanged(object sender, TextChangedEventArgs e)
		{
			if (!this.textbox.IsLoaded) {
				return;
			}

			// Value too long
			if (maxLength > 0 && this.textbox.Text.Length > maxLength) {
				this.UpdateField();

				return;
			}

			//int parsed = int.Parse(this.TextBox_Counter.Text);
			int parsed = this.number;
			bool success = int.TryParse(this.textbox.Text, out parsed);

			// Check minimal and maximal values unless they're equal (disabled)
			if (success && (this.minimum == this.maximum || (parsed >= this.minimum && parsed <= this.maximum))) {
				this.number = parsed;
			}

			// We have a valid numeric value, update the text field
			this.UpdateField();
		}

		#endregion
	}
}
