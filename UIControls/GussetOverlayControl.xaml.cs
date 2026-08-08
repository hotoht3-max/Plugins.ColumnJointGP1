using System.Windows;
using System.Windows.Controls;

namespace RAM.Plugins.ColumnJointGP1.UIControls
{
    // Делегат для события закрытия. Передает флаг "Нужно ли включить Мастера"
    public delegate void OverlayClosedEventHandler(object sender, bool shouldMasterBeChecked);


    public partial class GussetOverlayControl : UserControl
    {
        public event OverlayClosedEventHandler OverlayClosed;

        private MainWindowViewModel _viewModel;

        // ПАМЯТЬ СЛЕЙВ-ГАЛОЧЕК
        private bool _memMaterial;
        private bool _memPartPref;
        private bool _memPartNo;
        private bool _memAssyPref;
        private bool _memAssyNo;
        private bool _memName;
        private bool _memClass;
        private bool _memUDA;

        // ====================================================================
        // БЭКАП ДЛЯ КНОПКИ "ОТМЕНА" (Снимок состояния до начала редактирования)
        // ====================================================================
        private string _bckPartPref, _bckPartNo, _bckAssyPref, _bckAssyNo, _bckName, _bckThickness, _bckMaterial, _bckClass, _bckUDA;
        private bool _bckChkMaterial, _bckChkPartPref, _bckChkPartNo, _bckChkAssyPref, _bckChkAssyNo, _bckChkName, _bckChkClass, _bckChkUDA;

        public GussetOverlayControl()
        {
            InitializeComponent();

            // Прячем окно ТОЛЬКО при реальном запуске плагина. 
            // В Visual Studio дизайнер игнорирует событие Loaded, поэтому там окно останется видимым!
            this.Loaded += (s, e) =>
            {
                OverlayPanel.SetValue(System.Windows.Controls.Canvas.LeftProperty, -10000d);
            };
        }

        // Хакерский метод Теклы инкапсулирован здесь
        private void SetTeklaCheckbox(CheckBox chk, bool targetState)
        {
            if (chk.IsChecked != targetState)
            {
                chk.IsChecked = targetState;
                chk.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
            }
        }

        // API 1: Главное окно командует открыться
        public void Open(MainWindowViewModel viewModel)
        {
            _viewModel = viewModel;

            // 1. СОЗДАЕМ РЕЗЕРВНУЮ КОПИЮ ТЕКСТОВ
            _bckPartPref = _viewModel.GP_PartPref;
            _bckPartNo = _viewModel.GP_PartNo;
            _bckAssyPref = _viewModel.GP_AssyPref;
            _bckAssyNo = _viewModel.GP_AssyNo;
            _bckName = _viewModel.GP_Name;
            _bckThickness = _viewModel.GP_Thickness;
            _bckMaterial = _viewModel.GP_Material;
            _bckClass = _viewModel.GP_Class;
            _bckUDA = _viewModel.GP_UDA;

            // 2. СОЗДАЕМ РЕЗЕРВНУЮ КОПИЮ ГАЛОЧЕК
            _bckChkMaterial = chk_GP_Material.IsChecked == true;
            _bckChkPartPref = chk_GP_PartPref.IsChecked == true;
            _bckChkPartNo = chk_GP_PartNo.IsChecked == true;
            _bckChkAssyPref = chk_GP_AssyPref.IsChecked == true;
            _bckChkAssyNo = chk_GP_AssyNo.IsChecked == true;
            _bckChkName = chk_GP_Name.IsChecked == true;
            _bckChkClass = chk_GP_Class.IsChecked == true;
            _bckChkUDA = chk_GP_UDA.IsChecked == true;

            // 3. Парсим UDA для красивого отображения в строках
            bool[] udaStates = { chk_GP_UDA.IsChecked == true, chk_GP_UDA.IsChecked == true, chk_GP_UDA.IsChecked == true, chk_GP_UDA.IsChecked == true };
            var rows = Services.UdaParser.Parse(_viewModel.GP_UDA, udaStates);
            for (int i = 0; i < 4; i++)
            {
                _viewModel.UdaRows[i].IsChecked = rows[i].IsChecked;
                _viewModel.UdaRows[i].Name = rows[i].Name;
                _viewModel.UdaRows[i].Value = rows[i].Value;
            }

            OverlayPanel.SetValue(Canvas.LeftProperty, 0d);
            OverlayCanvas.IsHitTestVisible = true;
        }

        // НОВЫЙ МЕТОД: Обработка кнопки "Отмена"
        private void BtnCancelOverlay_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                // 1. Возвращаем старые тексты в ядро (важно делать это ДО галочек, 
                // так как смена текста может заставить Теклу автоматически поставить галочку)
                _viewModel.GP_PartPref = _bckPartPref;
                _viewModel.GP_PartNo = _bckPartNo;
                _viewModel.GP_AssyPref = _bckAssyPref;
                _viewModel.GP_AssyNo = _bckAssyNo;
                _viewModel.GP_Name = _bckName;
                _viewModel.GP_Thickness = _bckThickness;
                _viewModel.GP_Material = _bckMaterial;
                _viewModel.GP_Class = _bckClass;
                _viewModel.GP_UDA = _bckUDA;
            }

            // 2. Восстанавливаем старые галочки через симуляцию клика
            SetTeklaCheckbox(chk_GP_Material, _bckChkMaterial);
            SetTeklaCheckbox(chk_GP_PartPref, _bckChkPartPref);
            SetTeklaCheckbox(chk_GP_PartNo, _bckChkPartNo);
            SetTeklaCheckbox(chk_GP_AssyPref, _bckChkAssyPref);
            SetTeklaCheckbox(chk_GP_AssyNo, _bckChkAssyNo);
            SetTeklaCheckbox(chk_GP_Name, _bckChkName);
            SetTeklaCheckbox(chk_GP_Class, _bckChkClass);
            SetTeklaCheckbox(chk_GP_UDA, _bckChkUDA);

            // 3. Просто прячем окно. Никакие настройки каскадов не трогаем, 
            // событие OverlayClosed НЕ вызываем (Мастер на главном окне остается в прежнем виде).
            OverlayPanel.SetValue(Canvas.LeftProperty, -10000d);
            OverlayCanvas.IsHitTestVisible = false;
        }

        // API 2: Главное окно передает статус Мастер-галочки
        public void ApplyMasterState(bool isMasterChecked)
        {
            if (isMasterChecked)
            {
                SetTeklaCheckbox(chk_GP_Material, _memMaterial);
                SetTeklaCheckbox(chk_GP_PartPref, _memPartPref);
                SetTeklaCheckbox(chk_GP_PartNo, _memPartNo);
                SetTeklaCheckbox(chk_GP_AssyPref, _memAssyPref);
                SetTeklaCheckbox(chk_GP_AssyNo, _memAssyNo);
                SetTeklaCheckbox(chk_GP_Name, _memName);
                SetTeklaCheckbox(chk_GP_Class, _memClass);
                SetTeklaCheckbox(chk_GP_UDA, _memUDA);
            }
            else
            {
                _memMaterial = chk_GP_Material.IsChecked == true;
                _memPartPref = chk_GP_PartPref.IsChecked == true;
                _memPartNo = chk_GP_PartNo.IsChecked == true;
                _memAssyPref = chk_GP_AssyPref.IsChecked == true;
                _memAssyNo = chk_GP_AssyNo.IsChecked == true;
                _memName = chk_GP_Name.IsChecked == true;
                _memClass = chk_GP_Class.IsChecked == true;
                _memUDA = chk_GP_UDA.IsChecked == true;

                SetTeklaCheckbox(chk_GP_Material, false);
                SetTeklaCheckbox(chk_GP_PartPref, false);
                SetTeklaCheckbox(chk_GP_PartNo, false);
                SetTeklaCheckbox(chk_GP_AssyPref, false);
                SetTeklaCheckbox(chk_GP_AssyNo, false);
                SetTeklaCheckbox(chk_GP_Name, false);
                SetTeklaCheckbox(chk_GP_Class, false);
                SetTeklaCheckbox(chk_GP_UDA, false);
            }
        }

        private void BtnToggleAllOverlay_Click(object sender, RoutedEventArgs e)
        {
            bool hasChecks = chk_GP_Material.IsChecked == true || chk_GP_PartPref.IsChecked == true ||
                             chk_GP_PartNo.IsChecked == true || chk_GP_AssyPref.IsChecked == true ||
                             chk_GP_AssyNo.IsChecked == true || chk_GP_Name.IsChecked == true ||
                             chk_GP_Class.IsChecked == true || chk_GP_UDA.IsChecked == true;

            bool targetState = !hasChecks;

            SetTeklaCheckbox(chk_GP_Material, targetState);
            SetTeklaCheckbox(chk_GP_PartPref, targetState);
            SetTeklaCheckbox(chk_GP_PartNo, targetState);
            SetTeklaCheckbox(chk_GP_AssyPref, targetState);
            SetTeklaCheckbox(chk_GP_AssyNo, targetState);
            SetTeklaCheckbox(chk_GP_Name, targetState);
            SetTeklaCheckbox(chk_GP_Class, targetState);
            SetTeklaCheckbox(chk_GP_UDA, targetState);

            if (_viewModel != null)
            {
                foreach (var row in _viewModel.UdaRows) row.IsChecked = targetState;
            }
        }

        private void BtnCloseOverlay_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.GP_UDA = Services.UdaParser.Build(_viewModel.UdaRows);
                bool anyUdaChecked = false;
                foreach (var row in _viewModel.UdaRows)
                {
                    if (row.IsChecked && !string.IsNullOrWhiteSpace(row.Name)) anyUdaChecked = true;
                }
                SetTeklaCheckbox(chk_GP_UDA, anyUdaChecked);
            }

            bool hasInternalChecks = chk_GP_Material.IsChecked == true || chk_GP_PartPref.IsChecked == true ||
                                     chk_GP_PartNo.IsChecked == true || chk_GP_AssyPref.IsChecked == true ||
                                     chk_GP_AssyNo.IsChecked == true || chk_GP_Name.IsChecked == true ||
                                     chk_GP_Class.IsChecked == true || chk_GP_UDA.IsChecked == true;

            // Запоминаем текущие чекбоксы
            _memMaterial = chk_GP_Material.IsChecked == true;
            _memPartPref = chk_GP_PartPref.IsChecked == true;
            _memPartNo = chk_GP_PartNo.IsChecked == true;
            _memAssyPref = chk_GP_AssyPref.IsChecked == true;
            _memAssyNo = chk_GP_AssyNo.IsChecked == true;
            _memName = chk_GP_Name.IsChecked == true;
            _memClass = chk_GP_Class.IsChecked == true;
            _memUDA = chk_GP_UDA.IsChecked == true;

            // Прячем окно
            OverlayPanel.SetValue(Canvas.LeftProperty, -10000d);
            OverlayCanvas.IsHitTestVisible = false;

            // Отправляем событие главному окну: "Нужно ли зажечь Мастера?"
            OverlayClosed?.Invoke(this, hasInternalChecks);
        }

    }
}