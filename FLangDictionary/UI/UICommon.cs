using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FLangDictionary.UI
{
    class UICommon
    {
    }

    static class WindowExtensions
    {
        // Получает имя ресурса - строка с разными вариантами перевода, в зависимости от текущего языка.
        // Эти ресурсы лежат в Resources/Lang*.xaml
        public static string Lang(this FrameworkElement frameworkElement, string langResource)
        {
            return (string)frameworkElement.FindResource("Lang." + langResource);
        }
    }
}
