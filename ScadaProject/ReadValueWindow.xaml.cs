using System.Windows;

namespace ScadaProject
{
    public partial class ReadValueWindow : Window
    {
        private int index = -1;

        public ReadValueWindow()
        {
            InitializeComponent();
        }

        public int GetIndex()
        {
            return index;
        }

        private void Read(object sender, RoutedEventArgs e)
        {
            index = deviceComboBox.SelectedIndex;
            Close();
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            index = -1;
            Close();
        }
    }
}
