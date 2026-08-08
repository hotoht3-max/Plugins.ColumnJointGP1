using Tekla.Structures.Dialog;
using TD = Tekla.Structures.Datatype;
using System.Collections.ObjectModel;
using RAM.Plugins.ColumnJointGP1.Services;
using RAM.Plugins.ColumnJointGP1.Models;

namespace RAM.Plugins.ColumnJointGP1
{
    public class MainWindowViewModel : BaseViewModel
    {
        private double _offset_Web = 20.0;
        private int _offset_Web_Mode = 0;
        private double _offset_Gusset = 0.0;
        private double _offset_Brace = 50.0;

        private string _angle_Top = "";
        private double _straight_Top = 20.0;
        private string _angle_Bot = "";
        private double _straight_Bot = 20.0;

        private string _b1_Class = "110"; private double _b1_h = 150.0; private double _b1_e1 = 30.0; private double _b1_e2 = 30.0; private int _b1_wType = 1; private double _b1_wSize = 6.0;
        private string _b2_Class = ""; private double _b2_h = 150.0; private double _b2_e1 = 30.0; private double _b2_e2 = 30.0; private int _b2_wType = 1; private double _b2_wSize = 6.0;
        private string _b3_Class = ""; private double _b3_h = 150.0; private double _b3_e1 = 30.0; private double _b3_e2 = 30.0; private int _b3_wType = 1; private double _b3_wSize = 6.0;
        private string _b4_Class = ""; private double _b4_h = 150.0; private double _b4_e1 = 30.0; private double _b4_e2 = 30.0; private int _b4_wType = 1; private double _b4_wSize = 6.0;
        private string _b5_Class = ""; private double _b5_h = 150.0; private double _b5_e1 = 30.0; private double _b5_e2 = 30.0; private int _b5_wType = 1; private double _b5_wSize = 6.0;

        private int _spliceConnType = 0;
        private double _splice_h = 150.0;
        private double _splice_e1 = 30.0;
        private double _splice_e2 = 30.0;
        private int _splice_wType = 1;
        private double _splice_wSize = 6.0;

        private int _gusset_wType = 1;
        private double _gusset_wSize = 6.0;

        private string _class_Exclude = "99";
        private string _class_Splice = "15";

        private string _gp_Thickness = "10";
        private string _gp_Material = "C245";
        private string _gp_PartPref = "Ф";
        private string _gp_PartNo = "1";
        private string _gp_AssyPref = "С";
        private string _gp_AssyNo = "1";
        private string _gp_Name = "ФАСОНКА";
        private string _gp_Class = "100";
        private string _gp_UDA = "";

        private int _gusset_Shape_Mode = 1;
        private string _gusset_Rounding = "";
        private int _gp_PlanPos = 0;
        private int _hound_Enabled = 0;
        private double _hound_Distance = 500.0;

        private double _spliceBolt_Size = 20.0;
        private string _spliceBolt_Standard = "7798";
        private double _spliceBolt_Tol = 2.0;
        private int _spliceBolt_W1 = 1;
        private int _spliceBolt_W2 = 0;
        private int _spliceBolt_W3 = 1;
        private int _spliceBolt_N1 = 1;
        private int _spliceBolt_N2 = 0;
        private int _spliceBolt_Bolt = 1;

        private double _spliceBolt_Edge1 = 40.0;
        private string _spliceBolt_DistX = "70";
        private double _spliceBolt_Edge2 = 40.0;
        private string _spliceBolt_DistY = "60";
        private double _spliceBolt_Offset = 0.0;

        public ObservableCollection<string> AvailableStandards { get; } = new ObservableCollection<string>();
        public ObservableCollection<double> AvailableSizes { get; } = new ObservableCollection<double>();

        // Список стандартных классов для выпадающего меню (0 - 14)
        public ObservableCollection<string> StandardClasses { get; } = new ObservableCollection<string>
        { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14" };

        // Коллекция для привязки 4-х строк UDA в нашем Overlay-окне
        public ObservableCollection<UdaRow> UdaRows { get; } = new ObservableCollection<UdaRow>();

        public MainWindowViewModel()
        {
            RefreshStandards();
            // Инициализируем 4 пустые строки для формы UDA
            for (int i = 0; i < 4; i++) UdaRows.Add(new UdaRow());
        }

        [StructuresDialog("Offset_Web", typeof(TD.Double))] public double Offset_Web { get => _offset_Web; set => Set(ref _offset_Web, value); }
        [StructuresDialog("Offset_Web_Mode", typeof(TD.Integer))] public int Offset_Web_Mode { get => _offset_Web_Mode; set => Set(ref _offset_Web_Mode, value); }
        [StructuresDialog("Offset_Gusset", typeof(TD.Double))] public double Offset_Gusset { get => _offset_Gusset; set => Set(ref _offset_Gusset, value); }
        [StructuresDialog("Offset_Brace", typeof(TD.Double))] public double Offset_Brace { get => _offset_Brace; set => Set(ref _offset_Brace, value); }

        [StructuresDialog("Angle_Top", typeof(TD.String))] public string Angle_Top { get => _angle_Top; set => Set(ref _angle_Top, value); }
        [StructuresDialog("Straight_Top", typeof(TD.Double))] public double Straight_Top { get => _straight_Top; set => Set(ref _straight_Top, value); }
        [StructuresDialog("Angle_Bot", typeof(TD.String))] public string Angle_Bot { get => _angle_Bot; set => Set(ref _angle_Bot, value); }
        [StructuresDialog("Straight_Bot", typeof(TD.Double))] public double Straight_Bot { get => _straight_Bot; set => Set(ref _straight_Bot, value); }

        [StructuresDialog("B1_Class", typeof(TD.String))] public string B1_Class { get => _b1_Class; set => Set(ref _b1_Class, value); }
        [StructuresDialog("B1_h", typeof(TD.Double))] public double B1_h { get => _b1_h; set => Set(ref _b1_h, value); }
        [StructuresDialog("B1_e1", typeof(TD.Double))] public double B1_e1 { get => _b1_e1; set => Set(ref _b1_e1, value); }
        [StructuresDialog("B1_e2", typeof(TD.Double))] public double B1_e2 { get => _b1_e2; set => Set(ref _b1_e2, value); }
        [StructuresDialog("B1_WType", typeof(TD.Integer))] public int B1_WType { get => _b1_wType; set => Set(ref _b1_wType, value); }
        [StructuresDialog("B1_WSize", typeof(TD.Double))] public double B1_WSize { get => _b1_wSize; set => Set(ref _b1_wSize, value); }

        [StructuresDialog("B2_Class", typeof(TD.String))] public string B2_Class { get => _b2_Class; set => Set(ref _b2_Class, value); }
        [StructuresDialog("B2_h", typeof(TD.Double))] public double B2_h { get => _b2_h; set => Set(ref _b2_h, value); }
        [StructuresDialog("B2_e1", typeof(TD.Double))] public double B2_e1 { get => _b2_e1; set => Set(ref _b2_e1, value); }
        [StructuresDialog("B2_e2", typeof(TD.Double))] public double B2_e2 { get => _b2_e2; set => Set(ref _b2_e2, value); }
        [StructuresDialog("B2_WType", typeof(TD.Integer))] public int B2_WType { get => _b2_wType; set => Set(ref _b2_wType, value); }
        [StructuresDialog("B2_WSize", typeof(TD.Double))] public double B2_WSize { get => _b2_wSize; set => Set(ref _b2_wSize, value); }

        [StructuresDialog("B3_Class", typeof(TD.String))] public string B3_Class { get => _b3_Class; set => Set(ref _b3_Class, value); }
        [StructuresDialog("B3_h", typeof(TD.Double))] public double B3_h { get => _b3_h; set => Set(ref _b3_h, value); }
        [StructuresDialog("B3_e1", typeof(TD.Double))] public double B3_e1 { get => _b3_e1; set => Set(ref _b3_e1, value); }
        [StructuresDialog("B3_e2", typeof(TD.Double))] public double B3_e2 { get => _b3_e2; set => Set(ref _b3_e2, value); }
        [StructuresDialog("B3_WType", typeof(TD.Integer))] public int B3_WType { get => _b3_wType; set => Set(ref _b3_wType, value); }
        [StructuresDialog("B3_WSize", typeof(TD.Double))] public double B3_WSize { get => _b3_wSize; set => Set(ref _b3_wSize, value); }

        [StructuresDialog("B4_Class", typeof(TD.String))] public string B4_Class { get => _b4_Class; set => Set(ref _b4_Class, value); }
        [StructuresDialog("B4_h", typeof(TD.Double))] public double B4_h { get => _b4_h; set => Set(ref _b4_h, value); }
        [StructuresDialog("B4_e1", typeof(TD.Double))] public double B4_e1 { get => _b4_e1; set => Set(ref _b4_e1, value); }
        [StructuresDialog("B4_e2", typeof(TD.Double))] public double B4_e2 { get => _b4_e2; set => Set(ref _b4_e2, value); }
        [StructuresDialog("B4_WType", typeof(TD.Integer))] public int B4_WType { get => _b4_wType; set => Set(ref _b4_wType, value); }
        [StructuresDialog("B4_WSize", typeof(TD.Double))] public double B4_WSize { get => _b4_wSize; set => Set(ref _b4_wSize, value); }

        [StructuresDialog("B5_Class", typeof(TD.String))] public string B5_Class { get => _b5_Class; set => Set(ref _b5_Class, value); }
        [StructuresDialog("B5_h", typeof(TD.Double))] public double B5_h { get => _b5_h; set => Set(ref _b5_h, value); }
        [StructuresDialog("B5_e1", typeof(TD.Double))] public double B5_e1 { get => _b5_e1; set => Set(ref _b5_e1, value); }
        [StructuresDialog("B5_e2", typeof(TD.Double))] public double B5_e2 { get => _b5_e2; set => Set(ref _b5_e2, value); }
        [StructuresDialog("B5_WType", typeof(TD.Integer))] public int B5_WType { get => _b5_wType; set => Set(ref _b5_wType, value); }
        [StructuresDialog("B5_WSize", typeof(TD.Double))] public double B5_WSize { get => _b5_wSize; set => Set(ref _b5_wSize, value); }

        [StructuresDialog("SpliceConnType", typeof(TD.Integer))] public int SpliceConnType { get => _spliceConnType; set => Set(ref _spliceConnType, value); }
        [StructuresDialog("Splice_h", typeof(TD.Double))] public double Splice_h { get => _splice_h; set => Set(ref _splice_h, value); }
        [StructuresDialog("Splice_e1", typeof(TD.Double))] public double Splice_e1 { get => _splice_e1; set => Set(ref _splice_e1, value); }
        [StructuresDialog("Splice_e2", typeof(TD.Double))] public double Splice_e2 { get => _splice_e2; set => Set(ref _splice_e2, value); }
        [StructuresDialog("Splice_WType", typeof(TD.Integer))] public int Splice_WType { get => _splice_wType; set => Set(ref _splice_wType, value); }
        [StructuresDialog("Splice_WSize", typeof(TD.Double))] public double Splice_WSize { get => _splice_wSize; set => Set(ref _splice_wSize, value); }

        [StructuresDialog("Gusset_WType", typeof(TD.Integer))] public int Gusset_WType { get => _gusset_wType; set => Set(ref _gusset_wType, value); }
        [StructuresDialog("Gusset_WSize", typeof(TD.Double))] public double Gusset_WSize { get => _gusset_wSize; set => Set(ref _gusset_wSize, value); }

        [StructuresDialog("Class_Exclude", typeof(TD.String))] public string Class_Exclude { get => _class_Exclude; set => Set(ref _class_Exclude, value); }
        [StructuresDialog("Class_Splice", typeof(TD.String))] public string Class_Splice { get => _class_Splice; set => Set(ref _class_Splice, value); }

        // --- ОСНОВНЫЕ ПАРАМЕТРЫ ФАСОНКИ ---
        [StructuresDialog("GP_Thickness", typeof(TD.String))] public string GP_Thickness { get => _gp_Thickness; set => Set(ref _gp_Thickness, value); }
        [StructuresDialog("GP_Material", typeof(TD.String))] public string GP_Material { get => _gp_Material; set => Set(ref _gp_Material, value); }
        [StructuresDialog("GP_PartPref", typeof(TD.String))] public string GP_PartPref { get => _gp_PartPref; set => Set(ref _gp_PartPref, value); }
        [StructuresDialog("GP_PartNo", typeof(TD.String))] public string GP_PartNo { get => _gp_PartNo; set => Set(ref _gp_PartNo, value); }
        [StructuresDialog("GP_AssyPref", typeof(TD.String))] public string GP_AssyPref { get => _gp_AssyPref; set => Set(ref _gp_AssyPref, value); }
        [StructuresDialog("GP_AssyNo", typeof(TD.String))] public string GP_AssyNo { get => _gp_AssyNo; set => Set(ref _gp_AssyNo, value); }
        [StructuresDialog("GP_Name", typeof(TD.String))] public string GP_Name { get => _gp_Name; set => Set(ref _gp_Name, value); }
        [StructuresDialog("GP_Class", typeof(TD.String))] public string GP_Class { get => _gp_Class; set => Set(ref _gp_Class, value); }
        [StructuresDialog("GP_UDA", typeof(TD.String))] public string GP_UDA { get => _gp_UDA; set => Set(ref _gp_UDA, value); }

        [StructuresDialog("Gusset_Shape_Mode", typeof(TD.Integer))] public int Gusset_Shape_Mode { get => _gusset_Shape_Mode; set => Set(ref _gusset_Shape_Mode, value); }
        [StructuresDialog("Gusset_Rounding", typeof(TD.String))] public string Gusset_Rounding { get => _gusset_Rounding; set => Set(ref _gusset_Rounding, value); }
        [StructuresDialog("GP_PlanPos", typeof(TD.Integer))] public int GP_PlanPos { get => _gp_PlanPos; set => Set(ref _gp_PlanPos, value); }
        [StructuresDialog("Hound_Enabled", typeof(TD.Integer))] public int Hound_Enabled { get => _hound_Enabled; set => Set(ref _hound_Enabled, value); }
        [StructuresDialog("Hound_Distance", typeof(TD.Double))] public double Hound_Distance { get => _hound_Distance; set => Set(ref _hound_Distance, value); }

        [StructuresDialog("SpliceBolt_Size", typeof(TD.Double))]
        public double SpliceBolt_Size { get => _spliceBolt_Size; set => Set(ref _spliceBolt_Size, value); }

        [StructuresDialog("SpliceBolt_Standard", typeof(TD.String))]
        public string SpliceBolt_Standard
        {
            get => _spliceBolt_Standard;
            set
            {
                Set(ref _spliceBolt_Standard, value);
                RefreshSizes(value);
            }
        }

        [StructuresDialog("SpliceBolt_Tol", typeof(TD.Double))] public double SpliceBolt_Tol { get => _spliceBolt_Tol; set => Set(ref _spliceBolt_Tol, value); }
        [StructuresDialog("SpliceBolt_W1", typeof(TD.Integer))] public int SpliceBolt_W1 { get => _spliceBolt_W1; set => Set(ref _spliceBolt_W1, value); }
        [StructuresDialog("SpliceBolt_W2", typeof(TD.Integer))] public int SpliceBolt_W2 { get => _spliceBolt_W2; set => Set(ref _spliceBolt_W2, value); }
        [StructuresDialog("SpliceBolt_W3", typeof(TD.Integer))] public int SpliceBolt_W3 { get => _spliceBolt_W3; set => Set(ref _spliceBolt_W3, value); }
        [StructuresDialog("SpliceBolt_N1", typeof(TD.Integer))] public int SpliceBolt_N1 { get => _spliceBolt_N1; set => Set(ref _spliceBolt_N1, value); }
        [StructuresDialog("SpliceBolt_N2", typeof(TD.Integer))] public int SpliceBolt_N2 { get => _spliceBolt_N2; set => Set(ref _spliceBolt_N2, value); }
        [StructuresDialog("SpliceBolt_Bolt", typeof(TD.Integer))] public int SpliceBolt_Bolt { get => _spliceBolt_Bolt; set => Set(ref _spliceBolt_Bolt, value); }

        [StructuresDialog("SpliceBolt_Edge1", typeof(TD.Double))] public double SpliceBolt_Edge1 { get => _spliceBolt_Edge1; set => Set(ref _spliceBolt_Edge1, value); }
        [StructuresDialog("SpliceBolt_DistX", typeof(TD.String))] public string SpliceBolt_DistX { get => _spliceBolt_DistX; set => Set(ref _spliceBolt_DistX, value); }
        [StructuresDialog("SpliceBolt_Edge2", typeof(TD.Double))] public double SpliceBolt_Edge2 { get => _spliceBolt_Edge2; set => Set(ref _spliceBolt_Edge2, value); }
        [StructuresDialog("SpliceBolt_DistY", typeof(TD.String))] public string SpliceBolt_DistY { get => _spliceBolt_DistY; set => Set(ref _spliceBolt_DistY, value); }
        [StructuresDialog("SpliceBolt_Offset", typeof(TD.Double))] public double SpliceBolt_Offset { get => _spliceBolt_Offset; set => Set(ref _spliceBolt_Offset, value); }

        private void RefreshStandards()
        {
            AvailableStandards.Clear();
            foreach (var std in BoltCatalogService.GetAvailableStandards())
            {
                AvailableStandards.Add(std);
            }

            if (AvailableStandards.Contains(_spliceBolt_Standard))
            {
                RefreshSizes(_spliceBolt_Standard);
            }
        }

        private void RefreshSizes(string standard)
        {
            AvailableSizes.Clear();
            foreach (var size in BoltCatalogService.GetAvailableSizes(standard))
            {
                AvailableSizes.Add(size);
            }
        }
    }
}