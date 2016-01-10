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
        public static string ShowDialog_CreateNewArticle(Window owner)
        {
            // Сначала нужно получить имя имя доступной новой статьи по умолчанию

            const string defaultFreeWorkspaceNameBase = "Article";
            int defaultFreeWorkspaceNameNumber = 1;

            string defaultFreeWorkspaceName;
            do
                defaultFreeWorkspaceName = $"{defaultFreeWorkspaceNameBase}{defaultFreeWorkspaceNameNumber++}";
            while (Global.CurrentWorkspace.ArticleNames.Contains(defaultFreeWorkspaceName));

            InputBoxWindow dialog = new InputBoxWindow(
                defaultFreeWorkspaceName,
                owner.Lang("NewArticleDialog.Title"),
                owner.Lang("NewArticleDialog.Label"),
                owner.Lang("CreateButtonCaption"),
                owner.Lang("CancelButtonCaption"),
                (input) =>
                {
                    if (input == string.Empty)
                        return owner.Lang("Error.Article.IllegalName");

                    if (Global.CurrentWorkspace.ArticleNames.Contains(input))
                        return owner.Lang("Error.Article.AlreadyExists");

                    return null;
                });

            dialog.Owner = owner;
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            return dialog.ShowDialog().Value ? dialog.Input : null;
        }
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
