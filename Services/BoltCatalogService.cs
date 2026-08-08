using System.Collections.Generic;
using Tekla.Structures.Catalogs;

namespace RAM.Plugins.ColumnJointGP1.Services
{
    public static class BoltCatalogService
    {
        /// <summary>
        /// Возвращает список всех доступных стандартов болтов из базы данных Tekla.
        /// </summary>
        public static IEnumerable<string> GetAvailableStandards()
        {
            var standards = new HashSet<string>();
            try
            {
                // Создаем экземпляр (исправление ошибки CS0120)
                var catalogHandler = new CatalogHandler();
                var boltEnumerator = catalogHandler.GetBoltItems();

                while (boltEnumerator.MoveNext())
                {
                    if (boltEnumerator.Current is BoltItem boltItem)
                    {
                        if (!string.IsNullOrEmpty(boltItem.Standard) && !standards.Contains(boltItem.Standard))
                        {
                            standards.Add(boltItem.Standard);
                        }
                    }
                }
            }
            catch
            {
                // Логирование ошибки при необходимости
            }
            return standards;
        }

        /// <summary>
        /// Возвращает отфильтрованный список чистых диаметров для выбранного стандарта.
        /// </summary>
        public static IEnumerable<double> GetAvailableSizes(string standard)
        {
            var sizes = new HashSet<double>();
            if (string.IsNullOrEmpty(standard)) return sizes;

            try
            {
                // Создаем экземпляр (исправление ошибки CS0120)
                var catalogHandler = new CatalogHandler();
                var boltEnumerator = catalogHandler.GetBoltItems();

                while (boltEnumerator.MoveNext())
                {
                    if (boltEnumerator.Current is BoltItem boltItem)
                    {
                        if (boltItem.Standard == standard)
                        {
                            // Свойство Size уже double! (исправление ошибки CS1503)
                            double dia = boltItem.Size;

                            if (dia > 0 && !sizes.Contains(dia))
                            {
                                sizes.Add(dia);
                            }
                        }
                    }
                }
            }
            catch
            {
                // Логирование ошибки при необходимости
            }
            return sizes;
        }
    }
}