using RAM.Plugins.ColumnJointGP1.Models;
using System.Collections.Generic;

namespace RAM.Plugins.ColumnJointGP1.Services
{
    public static class PluginDataMapper
    {
        public static JointData Map(PluginData data)
        {
            var jointData = new JointData
            {
                Offset_Web = data.Offset_Web,
                Offset_Web_Mode = data.Offset_Web_Mode,
                Offset_Gusset = data.Offset_Gusset,
                Offset_Brace = data.Offset_Brace,

                Gusset_Shape_Mode = data.Gusset_Shape_Mode,
                // Маппинг Ищейки
                HoundEnabled = data.Hound_Enabled,
                HoundDistance = data.Hound_Distance,

                Angle_Top = data.Angle_Top,
                Straight_Top = data.Straight_Top,
                Angle_Bot = data.Angle_Bot,
                Straight_Bot = data.Straight_Bot,

                Class_Exclude = data.Class_Exclude,
                Class_Splice = data.Class_Splice,

                BraceTypes = new List<BraceSettings>(),

                GussetPlate = new PartSettings
                {
                    Profile = $"PL{data.GP_Thickness}",
                    Material = data.GP_Material,
                    PartPrefix = data.GP_PartPref,
                    PartStartNo = ParseInt(data.GP_PartNo),
                    AssemblyPrefix = data.GP_AssyPref,
                    AssemblyStartNo = ParseInt(data.GP_AssyNo),
                    Name = data.GP_Name,
                    Class = data.GP_Class,
                    UDA = data.GP_UDA
                }
            };

            // Парсинг округления: если текст пустой или кривой, оставляем null
            if (double.TryParse(data.Gusset_Rounding, out double roundingVal))
            {
                jointData.GussetRounding = roundingVal;
            }
            else
            {
                jointData.GussetRounding = null;
            }

            AddBraceSetting(jointData.BraceTypes, data.B1_Class, data.B1_h, data.B1_e1, data.B1_e2);

            AddBraceSetting(jointData.BraceTypes, data.B1_Class, data.B1_h, data.B1_e1, data.B1_e2);
            AddBraceSetting(jointData.BraceTypes, data.B2_Class, data.B2_h, data.B2_e1, data.B2_e2);
            AddBraceSetting(jointData.BraceTypes, data.B3_Class, data.B3_h, data.B3_e1, data.B3_e2);
            AddBraceSetting(jointData.BraceTypes, data.B4_Class, data.B4_h, data.B4_e1, data.B4_e2);
            AddBraceSetting(jointData.BraceTypes, data.B5_Class, data.B5_h, data.B5_e1, data.B5_e2);

            return jointData;
        }

        private static void AddBraceSetting(List<BraceSettings> list, string cls, double h, double e1, double e2)
        {
            if (!string.IsNullOrWhiteSpace(cls))
            {
                list.Add(new BraceSettings { Class = cls.Trim(), h = h, e1 = e1, e2 = e2 });
            }
        }

        private static int ParseInt(string value, int fallback = 1)
        {
            return int.TryParse(value, out int result) ? result : fallback;
        }
    }
}