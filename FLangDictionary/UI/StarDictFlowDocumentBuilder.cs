using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Input;
using System.Xml;

namespace FLangDictionary.UI
{
    // Преобразователь из строки в формате определения(перевода) из StarDict в формат FlowDocument разметки
    // Этот класс занимается интерпретацией тегов разметки и форматированием элемента FlowDocument на основе этой интерпретации
    public static class StarDictFlowDocumentBuilder
    {
        public static void Build(string termDescription, FlowDocument document, MouseButtonEventHandler termReferenceMouseUpHandler)
        {
            document.Blocks.Clear();

            Paragraph paragraph = new Paragraph();
            document.Blocks.Add(paragraph);

            XmlDocument dd = new XmlDocument();
            dd.LoadXml("<XMLRoot>" + termDescription + "</XMLRoot>");

            paragraph.Inlines.Add(new Run(dd.InnerText));
        }
    }
}
