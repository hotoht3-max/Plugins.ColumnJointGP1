using RAM.Plugins.ColumnJointGP1.Models;
using RAM.Plugins.ColumnJointGP1.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using Tekla.Structures.Model;
using Tekla.Structures.Model.UI;
using Tekla.Structures.Plugins;

namespace RAM.Plugins.ColumnJointGP1
{
    public class PluginData
    {
        [StructuresField("Offset_Web")] public double Offset_Web = 20.0;
        [StructuresField("Offset_Web_Mode")] public int Offset_Web_Mode = 0;
        [StructuresField("Offset_Gusset")] public double Offset_Gusset = 0.0;
        [StructuresField("Offset_Brace")] public double Offset_Brace = 50.0;

        [StructuresField("Angle_Top")] public string Angle_Top = "";
        [StructuresField("Straight_Top")] public double Straight_Top = 20.0;
        [StructuresField("Angle_Bot")] public string Angle_Bot = "";
        [StructuresField("Straight_Bot")] public double Straight_Bot = 20.0;

        // Таблица раскосов
        [StructuresField("B1_Class")] public string B1_Class = "110";
        [StructuresField("B1_h")] public double B1_h = 150.0;
        [StructuresField("B1_e1")] public double B1_e1 = 30.0;
        [StructuresField("B1_e2")] public double B1_e2 = 30.0;

        [StructuresField("B2_Class")] public string B2_Class = "";
        [StructuresField("B2_h")] public double B2_h = 150.0;
        [StructuresField("B2_e1")] public double B2_e1 = 30.0;
        [StructuresField("B2_e2")] public double B2_e2 = 30.0;

        [StructuresField("B3_Class")] public string B3_Class = "";
        [StructuresField("B3_h")] public double B3_h = 150.0;
        [StructuresField("B3_e1")] public double B3_e1 = 30.0;
        [StructuresField("B3_e2")] public double B3_e2 = 30.0;

        [StructuresField("B4_Class")] public string B4_Class = "";
        [StructuresField("B4_h")] public double B4_h = 150.0;
        [StructuresField("B4_e1")] public double B4_e1 = 30.0;
        [StructuresField("B4_e2")] public double B4_e2 = 30.0;

        [StructuresField("B5_Class")] public string B5_Class = "";
        [StructuresField("B5_h")] public double B5_h = 150.0;
        [StructuresField("B5_e1")] public double B5_e1 = 30.0;
        [StructuresField("B5_e2")] public double B5_e2 = 30.0;

        [StructuresField("Class_Exclude")] public string Class_Exclude = "99";
        [StructuresField("Class_Splice")] public string Class_Splice = "15";

        [StructuresField("GP_Thickness")] public string GP_Thickness = "10";
        [StructuresField("GP_Material")] public string GP_Material = "C245";
        [StructuresField("GP_PartPref")] public string GP_PartPref = "Ф";
        [StructuresField("GP_PartNo")] public string GP_PartNo = "1";
        [StructuresField("GP_AssyPref")] public string GP_AssyPref = "С";
        [StructuresField("GP_AssyNo")] public string GP_AssyNo = "1";
        [StructuresField("GP_Name")] public string GP_Name = "ФАСОНКА";
        [StructuresField("GP_Class")] public string GP_Class = "100";
        [StructuresField("GP_UDA")] public string GP_UDA = "";
    }

    [Plugin("RAM_ColumnJointGP1")]
    [PluginUserInterface("RAM.Plugins.ColumnJointGP1.MainWindow")]
    public class ModelPlugin : PluginBase
    {
        private PluginData Data { get; set; }

        public ModelPlugin(PluginData data)
        {
            Data = data;
        }

        public override List<InputDefinition> DefineInput()
        {
            try
            {
                Picker picker = new Picker();
                var inputs = new List<InputDefinition>();

                ModelObject branch = picker.PickObject(Picker.PickObjectEnum.PICK_ONE_PART, "Выберите ветвь колонны");
                inputs.Add(new InputDefinition(branch.Identifier));

                ModelObjectEnumerator lacingEnum = picker.PickObjects(Picker.PickObjectsEnum.PICK_N_PARTS, "Выберите элементы решетки (уголки) и НАЖМИТЕ СРЕДНЮЮ КНОПКУ МЫШИ");

                var lacingIdentifiers = new ArrayList();
                while (lacingEnum.MoveNext())
                {
                    if (lacingEnum.Current != null)
                        lacingIdentifiers.Add(lacingEnum.Current.Identifier);
                }

                if (lacingIdentifiers.Count == 0)
                {
                    MessageBox.Show("Решетка не выбрана. Выполнение прервано.", "RAM BIM", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return new List<InputDefinition>();
                }

                inputs.Add(new InputDefinition(lacingIdentifiers));
                return inputs;
            }
            catch (Exception ex)
            {
                if (!ex.Message.Contains("User interrupt"))
                {
                    MessageBox.Show($"Ошибка DefineInput:\n{ex.Message}", "RAM BIM Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                return new List<InputDefinition>();
            }
        }

        public override bool Run(List<InputDefinition> Input)
        {
            try
            {
                Logger.Write("--- ЗАПУСК ПЛАГИНА (RUN) ---");

                if (Input == null || Input.Count < 2)
                {
                    Logger.Write("Ошибка: Input == null или Count < 2", LogLevel.Error);
                    return false;
                }

                Model model = new Model();

                if (!(Input[0].GetInput() is Tekla.Structures.Identifier branchId))
                {
                    Logger.Write("Ошибка: Input[0] не является Identifier.", LogLevel.Error);
                    return false;
                }

                if (!(model.SelectModelObject(branchId) is Part branch))
                {
                    Logger.Write("Ошибка: Ветвь не найдена в модели.", LogLevel.Error);
                    return false;
                }

                if (!(Input[1].GetInput() is ArrayList lacingIds))
                {
                    Logger.Write($"Ошибка: Input[1] не является ArrayList.", LogLevel.Error);
                    return false;
                }

                List<Part> lacings = new List<Part>();
                foreach (var idObj in lacingIds)
                {
                    if (idObj is Tekla.Structures.Identifier id && model.SelectModelObject(id) is Part p)
                    {
                        lacings.Add(p);
                    }
                }

                if (lacings.Count == 0)
                {
                    Logger.Write("Ошибка: Список деталей решетки пуст.", LogLevel.Error);
                    return false;
                }

                Logger.Write($"Извлечено деталей решетки: {lacings.Count}");

                JointData jointData = PluginDataMapper.Map(Data);
                NodeGeometryBuilder.BuildNode(branch, lacings, jointData);

                Logger.Write("--- ПЛАГИН УСПЕШНО ЗАВЕРШИЛ РАБОТУ ---", LogLevel.Success);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Критическая ошибка Run():\n{ex.Message}\n\nСтек вызовов:\n{ex.StackTrace}", "RAM BIM Дебаг", MessageBoxButton.OK, MessageBoxImage.Error);
                Logger.Write($"Критическая ошибка Run: {ex.Message}\n{ex.StackTrace}", LogLevel.Error);
                return false;
            }
        }
    }
}