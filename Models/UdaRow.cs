using System.ComponentModel;

namespace RAM.Plugins.ColumnJointGP1.Models
{
    public class UdaRow : BaseViewModel
    {
        private bool _isChecked;
        private string _name;
        private string _value;

        public bool IsChecked
        {
            get => _isChecked;
            set => Set(ref _isChecked, value);
        }

        public string Name
        {
            get => _name;
            set => Set(ref _name, value);
        }

        public string Value
        {
            get => _value;
            set => Set(ref _value, value);
        }
    }
}