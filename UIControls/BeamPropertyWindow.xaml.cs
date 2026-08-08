using System.Windows;

namespace RAM.Plugins.ColumnJointGP1.UIControls
{
    public partial class BeamPropertyWindow : Window
    {
        public BeamPropertyWindow()
        {
            InitializeComponent();
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            // Говорим главному окну, что результат положительный
            this.DialogResult = true;
            this.Close();
        }
    }
}