using System.Collections.ObjectModel;

namespace RAM.Plugins.ColumnJointGP1.Models
{
    public class PartPropertiesContext : BaseViewModel
    {
        // Флаги галочек
        private bool _isPartPrefChecked;
        private bool _isPartNoChecked;
        private bool _isAssyPrefChecked;
        private bool _isAssyNoChecked;
        private bool _isNameChecked;
        private bool _isProfileChecked;
        private bool _isMaterialChecked;
        private bool _isClassChecked;

        public bool IsPartPrefChecked { get => _isPartPrefChecked; set => Set(ref _isPartPrefChecked, value); }
        public bool IsPartNoChecked { get => _isPartNoChecked; set => Set(ref _isPartNoChecked, value); }
        public bool IsAssyPrefChecked { get => _isAssyPrefChecked; set => Set(ref _isAssyPrefChecked, value); }
        public bool IsAssyNoChecked { get => _isAssyNoChecked; set => Set(ref _isAssyNoChecked, value); }
        public bool IsNameChecked { get => _isNameChecked; set => Set(ref _isNameChecked, value); }
        public bool IsProfileChecked { get => _isProfileChecked; set => Set(ref _isProfileChecked, value); }
        public bool IsMaterialChecked { get => _isMaterialChecked; set => Set(ref _isMaterialChecked, value); }
        public bool IsClassChecked { get => _isClassChecked; set => Set(ref _isClassChecked, value); }

        // Текстовые значения
        private string _partPrefix;
        private string _partStartNo;
        private string _assyPrefix;
        private string _assyStartNo;
        private string _name;
        private string _profile;
        private string _material;
        private string _class;

        public string PartPrefix { get => _partPrefix; set => Set(ref _partPrefix, value); }
        public string PartStartNo { get => _partStartNo; set => Set(ref _partStartNo, value); }
        public string AssyPrefix { get => _assyPrefix; set => Set(ref _assyPrefix, value); }
        public string AssyStartNo { get => _assyStartNo; set => Set(ref _assyStartNo, value); }
        public string Name { get => _name; set => Set(ref _name, value); }
        public string Profile { get => _profile; set => Set(ref _profile, value); }
        public string Material { get => _material; set => Set(ref _material, value); }
        public string ClassStr { get => _class; set => Set(ref _class, value); }

        // Коллекция из 4 строк UDA
        public ObservableCollection<UdaRow> UdaRows { get; } = new ObservableCollection<UdaRow>();
    }
}