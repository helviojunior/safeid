using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using MigraDoc.DocumentObjectModel.Shapes;
using MigraDoc.DocumentObjectModel.Tables;

namespace SafeTrend.Report
{
    public class PDFReport: IDisposable
    {
        private Document document = null;

        public PDFReport(String title, String author)
        {
            // Create a new MigraDoc document
            document = new Document();
            document.Info.Title = title;
            document.Info.Subject = "";
            document.Info.Author = author;// "SafeTrend - SafeID v1.0";

            DefineStyles(document);

            DefineContentSection(document, title, "SafeTrend - Relatório gerado em " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
                        
        }

        public void AddH1(String text, Boolean PageBreakBefore)
        {
            document.LastSection.AddParagraph(text, (PageBreakBefore ? "Heading1" : "Heading1noB"));
        }

        public void AddH1(String text)
        {
            AddH1(text, true);
        }

        public void AddH2(String text)
        {
            document.LastSection.AddParagraph(text, "Heading2");
        }

        public void AddH3(String text)
        {
            document.LastSection.AddParagraph(text, "Heading2");
        }

        public void AddParagraph(String text, Int32 tabs, Int32 SpaceAfter, Boolean bold)
        {
            Paragraph paragraph = document.LastSection.AddParagraph(text, (tabs > 0 ? "tab" + tabs : "Normal"));
            paragraph.Format.Alignment = ParagraphAlignment.Justify;
            //paragraph.AddText(text);
            paragraph.Format.SpaceAfter = SpaceAfter;
            paragraph.Format.Font.Bold = bold;
        }

        public void AddParagraph(String text)
        {
            AddParagraph(text, 0, 0, false);
        }

        //paragraph.AddTab();

        public void SaveToFile(String filename)
        {
            MigraDoc.DocumentObjectModel.IO.DdlWriter.WriteToFile(document, "MigraDoc.mdddl");

            PdfDocumentRenderer renderer = new PdfDocumentRenderer(true, PdfSharp.Pdf.PdfFontEmbedding.Always);
            renderer.Document = document;

            renderer.RenderDocument();

            // Save the document...
            renderer.PdfDocument.Save(filename);
        }

        /// <summary>
        /// Defines page setup, headers, and footers.
        /// </summary>
        private void DefineContentSection(Document document, String textHeader, String textFooter)
        {

            Section section = document.AddSection();
            section.PageSetup.OddAndEvenPagesHeaderFooter = true;
            section.PageSetup.StartingNumber = 1;
            section.PageSetup.TopMargin = "2.3cm";
            section.PageSetup.BottomMargin = "1.5cm";
            section.PageSetup.LeftMargin = "1.27cm";
            section.PageSetup.RightMargin = "1.27cm";
            section.PageSetup.FooterDistance = "0cm";

            HeaderFooter header = section.Headers.Primary;

            System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(typeof(PDFReport));
            String basePath = Path.GetDirectoryName(asm.Location);


            Image image = header.AddImage(Path.Combine(basePath,"report-logo.png"));
            image.Height = "1cm";
            image.LockAspectRatio = true;
            image.RelativeVertical = RelativeVertical.Page;
            image.RelativeHorizontal = RelativeHorizontal.Margin;
            image.Top = "0.70cm";
            image.Left = ShapePosition.Left;
            image.WrapFormat.Style = WrapStyle.Through;

            // Create the text frame for the header
            TextFrame addressFrame = header.AddTextFrame();
            addressFrame.Height = "3.0cm";
            addressFrame.Width = "20cm";
            addressFrame.Left = ShapePosition.Right;
            addressFrame.RelativeHorizontal = RelativeHorizontal.Margin;
            addressFrame.Top = "1.40cm";
            addressFrame.RelativeVertical = RelativeVertical.Page;

            Paragraph headerParagraph = addressFrame.AddParagraph(textHeader);
            headerParagraph.Format.Font.Name = "Arial";
            headerParagraph.Format.Font.Size = 11;
            headerParagraph.Format.Alignment = ParagraphAlignment.Right;

            section.Headers.EvenPage.Add(image.Clone());
            section.Headers.EvenPage.Add(addressFrame.Clone());


            // Create a paragraph with centered page number. See definition of style "Footer".

            Paragraph paragraph = new Paragraph();
            //paragraph.Format.SpaceAfter = "0.1cm";
            paragraph.Format.Alignment = ParagraphAlignment.Center;
            paragraph.Format.Borders.Top.Color = new Color(150, 150, 150);
            paragraph.Format.Borders.Top.Visible = true;

            section.Footers.Primary.Add(paragraph);
            section.Footers.EvenPage.Add(paragraph.Clone());


            TextFrame tf = new TextFrame();
            tf.Width = "0.8cm";
            tf.Height = "0.5cm";
            tf.Left = ShapePosition.Right;
            tf.RelativeHorizontal = RelativeHorizontal.Margin;
            tf.RelativeVertical = RelativeVertical.Paragraph;
            tf.MarginTop = "0.2cm";

            paragraph = tf.AddParagraph();
            paragraph.Format.Font.Size = 10;
            paragraph.Format.Font.Color = new Color(150, 150, 150);
            paragraph.Format.Alignment = ParagraphAlignment.Left;
            paragraph.Format.Borders.Left.Visible = true;
            paragraph.Format.Borders.Left.Color = new Color(150, 150, 150);
            paragraph.Format.Borders.DistanceFromLeft = "0.1cm";

            paragraph.AddPageField();

            section.Footers.Primary.Add(tf);
            section.Footers.EvenPage.Add(tf.Clone());

            tf = new TextFrame();
            tf.Width = "10cm";
            tf.Height = "0.5cm";
            tf.Left = ShapePosition.Right;
            tf.RelativeHorizontal = RelativeHorizontal.Margin;
            tf.RelativeVertical = RelativeVertical.Paragraph;
            tf.MarginTop = "0.2cm";
            tf.MarginRight = "1.2cm";

            paragraph = tf.AddParagraph(textFooter);
            paragraph.Format.Font.Size = 9;
            paragraph.Format.Font.Color = new Color(200, 200, 200);
            paragraph.Format.Alignment = ParagraphAlignment.Right;

            section.Footers.Primary.Add(tf);
            section.Footers.EvenPage.Add(tf.Clone());
        }

        /// <summary>
        /// Defines the styles used in the document.
        /// </summary>
        private static void DefineStyles(Document document)
        {
            // Get the predefined style Normal.
            Style style = document.Styles["Normal"];
            // Because all styles are derived from Normal, the next line changes the 
            // font of the whole document. Or, more exactly, it changes the font of
            // all styles and paragraphs that do not redefine the font.
            style.Font.Name = "Arial";
            style.Font.Size = 10;

            // Heading1 to Heading9 are predefined styles with an outline level. An outline level
            // other than OutlineLevel.BodyText automatically creates the outline (or bookmarks) 
            // in PDF.


            style = document.Styles.AddStyle("tab1", "Normal");
            //style.ParagraphFormat.AddTabStop("5cm", TabAlignment.Right);
            style.ParagraphFormat.LeftIndent = "0.7cm";

            style = document.Styles.AddStyle("tab2", "Normal");
            style.ParagraphFormat.LeftIndent = "1.4cm";
            
            style = document.Styles.AddStyle("tab3", "Normal");
            style.ParagraphFormat.LeftIndent = "2.1cm";

            style = document.Styles.AddStyle("tab4", "Normal");
            style.ParagraphFormat.LeftIndent = "2.8cm";

            style = document.Styles["Heading1"];
            style.Font.Name = "Calibri Light";
            style.Font.Size = 16;
            style.Font.Bold = true;
            style.Font.Color = new Color(192, 0, 0);
            style.ParagraphFormat.PageBreakBefore = true;
            style.ParagraphFormat.SpaceAfter = 6;

            style = document.Styles.AddStyle("Heading1noB", "Normal");
            //style = document.Styles["Heading1noB"];
            style.Font.Name = "Calibri Light";
            style.Font.Size = 16;
            style.Font.Bold = true;
            style.Font.Color = new Color(192, 0, 0);
            style.ParagraphFormat.PageBreakBefore = false;
            style.ParagraphFormat.SpaceAfter = 6;


            style = document.Styles["Heading2"];
            style.Font.Size = 13;
            //style.Font.Bold = true;
            style.ParagraphFormat.PageBreakBefore = false;
            style.ParagraphFormat.SpaceBefore = 6;
            style.ParagraphFormat.SpaceAfter = 6;

            style = document.Styles["Heading3"];
            style.Font.Size = 10;
            //style.Font.Bold = true;
            //style.Font.Italic = true;
            style.ParagraphFormat.SpaceBefore = 6;
            style.ParagraphFormat.SpaceAfter = 3;

            //style = document.Styles[StyleNames.Header];
            //style.ParagraphFormat.AddTabStop("16cm", TabAlignment.Right);


            // Create a new style called TextBox based on style Normal
            style = document.Styles.AddStyle("TextBox", "Normal");
            style.ParagraphFormat.Alignment = ParagraphAlignment.Justify;
            style.ParagraphFormat.Borders.Width = 2.5;
            style.ParagraphFormat.Borders.Distance = "3pt";
            //TODO: Colors
            style.ParagraphFormat.Shading.Color = Colors.SkyBlue;

            // Create a new style called TOC based on style Normal
            style = document.Styles.AddStyle("TOC", "Normal");
            style.ParagraphFormat.AddTabStop("16cm", TabAlignment.Right, TabLeader.Dots);
            style.ParagraphFormat.Font.Color = Colors.Blue;

        }

        public void Dispose()
        {
            document = null;
        }
    }
}
