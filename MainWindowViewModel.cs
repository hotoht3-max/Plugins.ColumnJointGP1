using Tekla.Structures.Dialog;
using TD = Tekla.Structures.Datatype;

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

		private string _b1_Class = "110"; private double _b1_h = 150.0; private double _b1_e1 = 30.0; private double _b1_e2 = 30.0;
		private string _b2_Class = ""; private double _b2_h = 150.0; private double _b2_e1 = 30.0; private double _b2_e2 = 30.0;
		private string _b3_Class = ""; private double _b3_h = 150.0; private double _b3_e1 = 30.0; private double _b3_e2 = 30.0;
		private string _b4_Class = ""; private double _b4_h = 150.0; private double _b4_e1 = 30.0; private double _b4_e2 = 30.0;
		private string _b5_Class = ""; private double _b5_h = 150.0; private double _b5_e1 = 30.0; private double _b5_e2 = 30.0;

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
        // НОВЫЕ ПРИВАТНЫЕ ПОЛЯ:
        private string _gusset_Rounding = "";
        private int _hound_Enabled = 0;
        private double _hound_Distance = 500.0;

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

		[StructuresDialog("B2_Class", typeof(TD.String))] public string B2_Class { get => _b2_Class; set => Set(ref _b2_Class, value); }
		[StructuresDialog("B2_h", typeof(TD.Double))] public double B2_h { get => _b2_h; set => Set(ref _b2_h, value); }
		[StructuresDialog("B2_e1", typeof(TD.Double))] public double B2_e1 { get => _b2_e1; set => Set(ref _b2_e1, value); }
		[StructuresDialog("B2_e2", typeof(TD.Double))] public double B2_e2 { get => _b2_e2; set => Set(ref _b2_e2, value); }

		[StructuresDialog("B3_Class", typeof(TD.String))] public string B3_Class { get => _b3_Class; set => Set(ref _b3_Class, value); }
		[StructuresDialog("B3_h", typeof(TD.Double))] public double B3_h { get => _b3_h; set => Set(ref _b3_h, value); }
		[StructuresDialog("B3_e1", typeof(TD.Double))] public double B3_e1 { get => _b3_e1; set => Set(ref _b3_e1, value); }
		[StructuresDialog("B3_e2", typeof(TD.Double))] public double B3_e2 { get => _b3_e2; set => Set(ref _b3_e2, value); }

		[StructuresDialog("B4_Class", typeof(TD.String))] public string B4_Class { get => _b4_Class; set => Set(ref _b4_Class, value); }
		[StructuresDialog("B4_h", typeof(TD.Double))] public double B4_h { get => _b4_h; set => Set(ref _b4_h, value); }
		[StructuresDialog("B4_e1", typeof(TD.Double))] public double B4_e1 { get => _b4_e1; set => Set(ref _b4_e1, value); }
		[StructuresDialog("B4_e2", typeof(TD.Double))] public double B4_e2 { get => _b4_e2; set => Set(ref _b4_e2, value); }

		[StructuresDialog("B5_Class", typeof(TD.String))] public string B5_Class { get => _b5_Class; set => Set(ref _b5_Class, value); }
		[StructuresDialog("B5_h", typeof(TD.Double))] public double B5_h { get => _b5_h; set => Set(ref _b5_h, value); }
		[StructuresDialog("B5_e1", typeof(TD.Double))] public double B5_e1 { get => _b5_e1; set => Set(ref _b5_e1, value); }
		[StructuresDialog("B5_e2", typeof(TD.Double))] public double B5_e2 { get => _b5_e2; set => Set(ref _b5_e2, value); }

		[StructuresDialog("Class_Exclude", typeof(TD.String))] public string Class_Exclude { get => _class_Exclude; set => Set(ref _class_Exclude, value); }
		[StructuresDialog("Class_Splice", typeof(TD.String))] public string Class_Splice { get => _class_Splice; set => Set(ref _class_Splice, value); }

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
        // НОВЫЕ СВОЙСТВА:
        [StructuresDialog("Gusset_Rounding", typeof(TD.String))]
        public string Gusset_Rounding { get => _gusset_Rounding; set => Set(ref _gusset_Rounding, value); }

        [StructuresDialog("Hound_Enabled", typeof(TD.Integer))]
        public int Hound_Enabled { get => _hound_Enabled; set => Set(ref _hound_Enabled, value); }

        [StructuresDialog("Hound_Distance", typeof(TD.Double))]
        public double Hound_Distance { get => _hound_Distance; set => Set(ref _hound_Distance, value); }
    }
}