using System;
using Tekla.Structures.Dialog;
using RAM.Plugins.ColumnJointGP1.Models;
using RAM.Plugins.ColumnJointGP1.Services;
using RAM.Plugins.ColumnJointGP1.UIControls;

namespace RAM.Plugins.ColumnJointGP1
{
    public partial class MainWindow : PluginWindowBase
    {
        public MainWindowViewModel DataViewModel { get; set; }

        public MainWindow(MainWindowViewModel ViewModel)
        {
            InitializeComponent();
            DataViewModel = ViewModel;
            this.DataContext = DataViewModel;
        }

        private void WpfOkApplyModifyGetOnOffCancel_ApplyClicked(object sender, EventArgs e) => this.Apply();

        private void WpfOkApplyModifyGetOnOffCancel_CancelClicked(object sender, EventArgs e) => this.Close();

        private void WpfOkApplyModifyGetOnOffCancel_GetClicked(object sender, EventArgs e) => this.Get();

        private void WpfOkApplyModifyGetOnOffCancel_ModifyClicked(object sender, EventArgs e) => this.Modify();

        private void WpfOkApplyModifyGetOnOffCancel_OkClicked(object sender, EventArgs e)
        {
            this.Apply();
            this.Close();
        }

        private void WpfOkApplyModifyGetOnOffCancel_OnOffClicked(object sender, EventArgs e) => this.ToggleSelection();

        // 1. Плашка просит открыть Оверлей
        private void GussetPlate_OpenOverlayRequested(object sender, EventArgs e)
        {
            GussetOverlay.Open(DataViewModel);
        }

        // 2. Инженер переключил галочку на плашке -> передаем статус Оверлею
        private void GussetPlate_MasterCheckToggled(object sender, bool isChecked)
        {
            GussetOverlay.ApplyMasterState(isChecked);
        }

        // 3. Оверлей закрылся и говорит, что внутри есть настройки -> включаем галочку на плашке
        private void GussetOverlay_OverlayClosed(object sender, bool shouldMasterBeChecked)
        {
            if (!GussetPlate.IsMasterChecked && shouldMasterBeChecked)
            {
                GussetPlate.SetMasterState(true);
            }
        }
    }
}