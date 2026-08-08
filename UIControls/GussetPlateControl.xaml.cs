using System;
using System.Windows;
using System.Windows.Controls;

namespace RAM.Plugins.ColumnJointGP1.UIControls
{
    public partial class GussetPlateControl : UserControl
    {
        // 1. События, на которые сможет подписаться главное окно
        public event EventHandler OpenOverlayRequested;
        public event EventHandler<bool> MasterCheckToggled;

        public GussetPlateControl()
        {
            InitializeComponent();
        }

        // Свойство для проверки состояния галочки извне
        public bool IsMasterChecked => chk_GP_Thickness_Master.IsChecked == true;

        // API: Метод для программного включения Мастера из главного окна
        public void SetMasterState(bool isChecked)
        {
            if (chk_GP_Thickness_Master.IsChecked != isChecked)
            {
                chk_GP_Thickness_Master.IsChecked = isChecked;
                // Симулируем клик, чтобы Текла это прожевала
                chk_GP_Thickness_Master.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
            }
        }

        // Инженер нажал на кнопку-плашку
        private void BtnOpenOverlay_Click(object sender, RoutedEventArgs e)
        {
            OpenOverlayRequested?.Invoke(this, EventArgs.Empty);
        }

        // Инженер нажал на Мастер-галочку
        private void MasterCheckbox_Click(object sender, RoutedEventArgs e)
        {
            MasterCheckToggled?.Invoke(this, chk_GP_Thickness_Master.IsChecked == true);
        }
    }
}