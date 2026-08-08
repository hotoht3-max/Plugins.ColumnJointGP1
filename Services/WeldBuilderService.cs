using Tekla.Structures.Model;

namespace RAM.Plugins.ColumnJointGP1.Services
{
    public static class WeldBuilderService
    {
        /// <summary>
        /// Создает угловой шов по контуру между двумя деталями.
        /// </summary>
        /// <param name="mainPart">Главная деталь (к которой приваривают)</param>
        /// <param name="secondaryPart">Второстепенная деталь (которую приваривают)</param>
        /// <param name="wType">Тип: 0 - Нет, 1 - Заводской, 2 - Монтажный</param>
        /// <param name="wSize">Катет шва</param>
        public static void CreateFilletWeld(Part mainPart, Part secondaryPart, int wType, double wSize)
        {
            // Если выбран тип "0 - Нет" или детали отсутствуют, прерываем
            if (wType == 0 || mainPart == null || secondaryPart == null) return;

            Weld weld = new Weld();
            weld.MainObject = mainPart;
            weld.SecondaryObject = secondaryPart;

            // Угловой шов
            weld.TypeAbove = BaseWeld.WeldTypeEnum.WELD_TYPE_FILLET;
            weld.TypeBelow = BaseWeld.WeldTypeEnum.WELD_TYPE_FILLET;

            // Катет шва (сразу сверху и снизу для симметрии)
            weld.SizeAbove = wSize;
            weld.SizeBelow = wSize;

            // Обварка по контуру
            weld.AroundWeld = true;

            // Заводской (1) или Монтажный (2)
            weld.ShopWeld = (wType == 1);

            weld.Insert();
        }
    }
}