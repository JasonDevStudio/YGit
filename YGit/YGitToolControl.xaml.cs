using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using YGit.ViewModel;

namespace YGit
{
    /// <summary>
    /// Interaction logic for YGitToolControl.
    /// </summary>
    public partial class YGitToolControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="YGitToolControl"/> class.
        /// </summary>
        public YGitToolControl()
        {
            this.InitializeComponent();
            var vm = new YGitVM(); 
            vm.LoadConf();
            this.DataContext = vm;

        }

        /// <summary>
        /// Handles click on the button by displaying a message box.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions", Justification = "Sample code")]
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Default event handler naming pattern")]
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                string.Format(System.Globalization.CultureInfo.CurrentUICulture, "Invoked '{0}' 9999", this.ToString()),
                "YGitTool");
        }
    }
}