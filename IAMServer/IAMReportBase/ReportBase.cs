using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Data;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace IAM.Report
{

    [Serializable()]
    public class ReportBase: IDisposable
    {
        private DataTable _dt;
        private Dictionary<String, String> _columnsTitle;

        public DataTable data { get { return _dt; } set { _dt = value; } }

        public ReportBase(DataTable sourceData, Dictionary<String, String> columnsTitle) :
            this(sourceData)
        {
            this._columnsTitle = columnsTitle;
        }

        public ReportBase(DataTable sourceData) :
            this()
        {
            this._dt = sourceData.Clone();

            foreach (DataRow dr in sourceData.Rows)
            {
                this._dt.Rows.Add(dr.ItemArray);
            }
        }
        
        public ReportBase()
        {
            _dt = new DataTable();
            _columnsTitle = new Dictionary<string, string>();
        }

        public DataRow NewRow()
        {
            return _dt.NewRow();
        }


        public ReportBase CloneSchema()
        {
            ReportBase dts = new ReportBase();
            dts._dt = this._dt.Clone();
            dts._columnsTitle = this._columnsTitle;

            return dts;
        }

        public ReportBase Clone()
        {
            ReportBase dts = new ReportBase();
            dts._dt = this._dt.Clone();
            dts._columnsTitle = this._columnsTitle;

            foreach (DataRow dr in this._dt.Rows)
            {
                dts._dt.Rows.Add(dr.ItemArray);
            }

            return dts;
        }

        public void Dispose()
        {
            _columnsTitle.Clear();
            _dt.Dispose();
        }

        public ReportBase Select()
        {
            return Select("");
        }

        public ReportBase Select(String filter)
        {
            ReportBase dts = new ReportBase();
            dts._dt = this._dt.Clone();
            dts._columnsTitle = this._columnsTitle;

            foreach (DataRow dr in this._dt.Select(filter))
            {
                dts._dt.Rows.Add(dr.ItemArray);
            }

            return dts;
        }

        public void CopyTo(ReportBase data)
        {
            data._dt.Merge(this._dt);
            //this._dt.Merge(	data._dt);
        }

        public void AddColumn(DataColumn column, String title)
        {
            _dt.Columns.Add(column);
            _columnsTitle.Add(column.ColumnName, title);
        }

        public void AddColumn(DataColumn column)
        {
            AddColumn(column, column.ColumnName);
        }

        /*
        public void AddRow(DataRow data){
            _dt.Rows.Add(data);
        }*/

        public void AddRow(Object[] data)
        {
            _dt.Rows.Add(data);
        }

        public String GetXML(String WorksheetName, String filter)
        {
            //Verifica header das colunas
            foreach (DataColumn col in _dt.Columns)
            {
                if (!_columnsTitle.ContainsKey(col.ColumnName))
                    _columnsTitle.Add(col.ColumnName, col.ColumnName);
            }

            StringBuilder xmlData = new StringBuilder();
            xmlData.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            xmlData.AppendLine("<?mso-application progid=\"Excel.Sheet\"?>");
            xmlData.AppendLine("<Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\"");
            xmlData.AppendLine(" xmlns:o=\"urn:schemas-microsoft-com:office:office\"");
            xmlData.AppendLine(" xmlns:x=\"urn:schemas-microsoft-com:office:excel\"");
            xmlData.AppendLine(" xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\"");
            xmlData.AppendLine(" xmlns:html=\"http://www.w3.org/TR/REC-html40\">");
            xmlData.AppendLine(" <DocumentProperties xmlns=\"urn:schemas-microsoft-com:office:office\">");
            xmlData.AppendLine("  <Author>SafeID - IAM</Author>");
            xmlData.AppendLine("  <LastAuthor>SafeID - IAM</LastAuthor>");
            xmlData.AppendLine("  <Created>"+ DateTime.Now.ToString("YYYY-MM-ddTHHMMssZ") +"</Created>");
            xmlData.AppendLine("  <Company>SafeID - IAM</Company>");
            xmlData.AppendLine("  <Version>11.9999</Version>");
            xmlData.AppendLine(" </DocumentProperties>");
            xmlData.AppendLine(" <ExcelWorkbook xmlns=\"urn:schemas-microsoft-com:office:excel\">");
            xmlData.AppendLine("  <WindowHeight>8835</WindowHeight>");
            xmlData.AppendLine("  <WindowWidth>8460</WindowWidth>");
            xmlData.AppendLine("  <WindowTopX>480</WindowTopX>");
            xmlData.AppendLine("  <WindowTopY>120</WindowTopY>");
            xmlData.AppendLine("  <ProtectStructure>False</ProtectStructure>");
            xmlData.AppendLine("  <ProtectWindows>False</ProtectWindows>");
            xmlData.AppendLine(" </ExcelWorkbook>");
            xmlData.AppendLine(" <Styles>");
            xmlData.AppendLine("  <Style ss:ID=\"Default\" ss:Name=\"Normal\">");
            xmlData.AppendLine("   <Alignment ss:Vertical=\"Bottom\"/>");
            xmlData.AppendLine("   <Borders/>");
            xmlData.AppendLine("   <Font/>");
            xmlData.AppendLine("   <Interior/>");
            xmlData.AppendLine("   <NumberFormat/>");
            xmlData.AppendLine("   <Protection/>");
            xmlData.AppendLine("  </Style>");
            xmlData.AppendLine("  <Style ss:ID=\"s21\">");
            xmlData.AppendLine("   <Alignment ss:Horizontal=\"Center\" ss:Vertical=\"Bottom\"/>");
            xmlData.AppendLine("  </Style>");
            xmlData.AppendLine("  <Style ss:ID=\"s23\">");
            xmlData.AppendLine("   <Font x:Family=\"Swiss\" ss:Bold=\"1\"/>");
            xmlData.AppendLine("   <Interior ss:Color=\"#C0C0C0\" ss:Pattern=\"Solid\"/>");
            xmlData.AppendLine("  </Style>");
            xmlData.AppendLine("  <Style ss:ID=\"s24\">");
            xmlData.AppendLine("   <Alignment ss:Horizontal=\"Center\" ss:Vertical=\"Bottom\"/>");
            xmlData.AppendLine("   <Borders>");
            xmlData.AppendLine("    <Border ss:Position=\"Bottom\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>");
            xmlData.AppendLine("    <Border ss:Position=\"Left\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>");
            xmlData.AppendLine("    <Border ss:Position=\"Right\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>");
            xmlData.AppendLine("    <Border ss:Position=\"Top\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>");
            xmlData.AppendLine("   </Borders>");
            xmlData.AppendLine("   <Font x:Family=\"Swiss\" ss:Bold=\"1\"/>");
            xmlData.AppendLine("   <Interior ss:Color=\"#C0C0C0\" ss:Pattern=\"Solid\"/>");
            xmlData.AppendLine("  </Style>");
            xmlData.AppendLine("  <Style ss:ID=\"s25\">");
            xmlData.AppendLine("   <Alignment ss:Horizontal=\"Center\" ss:Vertical=\"Bottom\"/>");
            xmlData.AppendLine("   <Borders>");
            xmlData.AppendLine("    <Border ss:Position=\"Bottom\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>");
            xmlData.AppendLine("    <Border ss:Position=\"Left\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>");
            xmlData.AppendLine("    <Border ss:Position=\"Right\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>");
            xmlData.AppendLine("    <Border ss:Position=\"Top\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>");
            xmlData.AppendLine("   </Borders>");
            xmlData.AppendLine("  </Style>");
            xmlData.AppendLine("  <Style ss:ID=\"s26\">");
            xmlData.AppendLine("   <Alignment ss:Horizontal=\"Center\" ss:Vertical=\"Bottom\"/>");
            xmlData.AppendLine("   <Borders>");
            xmlData.AppendLine("    <Border ss:Position=\"Bottom\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>");
            xmlData.AppendLine("    <Border ss:Position=\"Left\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>");
            xmlData.AppendLine("    <Border ss:Position=\"Right\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>");
            xmlData.AppendLine("    <Border ss:Position=\"Top\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>");
            xmlData.AppendLine("   </Borders>");
            xmlData.AppendLine("   <NumberFormat ss:Format=\"Short Date\"/>");
            xmlData.AppendLine("  </Style>");
            xmlData.AppendLine("  <Style ss:ID=\"s27\">");
            xmlData.AppendLine("   <Alignment ss:Horizontal=\"Left\" ss:Vertical=\"Bottom\"/>");
            xmlData.AppendLine("   <Borders>");
            xmlData.AppendLine("    <Border ss:Position=\"Bottom\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>");
            xmlData.AppendLine("    <Border ss:Position=\"Left\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>");
            xmlData.AppendLine("    <Border ss:Position=\"Right\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>");
            xmlData.AppendLine("    <Border ss:Position=\"Top\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>");
            xmlData.AppendLine("   </Borders>");
            xmlData.AppendLine("  </Style>");
            xmlData.AppendLine("  <Style ss:ID=\"s28\">");
            xmlData.AppendLine("   <Alignment ss:Horizontal=\"Left\" ss:Vertical=\"Bottom\"/>");
            xmlData.AppendLine("   <Borders>");
            xmlData.AppendLine("    <Border ss:Position=\"Bottom\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>");
            xmlData.AppendLine("    <Border ss:Position=\"Left\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>");
            xmlData.AppendLine("    <Border ss:Position=\"Right\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>");
            xmlData.AppendLine("    <Border ss:Position=\"Top\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>");
            xmlData.AppendLine("   </Borders>");
            xmlData.AppendLine("   <NumberFormat ss:Format=\"Short Date\"/>");
            xmlData.AppendLine("  </Style>");
            xmlData.AppendLine("  <Style ss:ID=\"s29\">");
            xmlData.AppendLine("   <Alignment ss:Horizontal=\"Left\" ss:Vertical=\"Bottom\"/>");
            xmlData.AppendLine("   <Borders>");
            xmlData.AppendLine("    <Border ss:Position=\"Bottom\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>");
            xmlData.AppendLine("    <Border ss:Position=\"Left\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>");
            xmlData.AppendLine("    <Border ss:Position=\"Right\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>");
            xmlData.AppendLine("    <Border ss:Position=\"Top\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>");
            xmlData.AppendLine("   </Borders>");
            xmlData.AppendLine("  </Style>");
            xmlData.AppendLine(" </Styles>");
            xmlData.AppendLine("<Worksheet ss:Name=\"" + WorksheetName + "\">");
            xmlData.AppendLine("<Table ss:ExpandedColumnCount=\"" + _dt.Columns.Count + "\" ss:ExpandedRowCount=\"" + (((Int32)(_dt.Rows.Count)) + 1) + "\" x:FullColumns=\"1\" x:FullRows=\"1\">");

            foreach (DataColumn col in _dt.Columns)
            {
                xmlData.AppendLine("   <Column ss:StyleID=\"s21\" ss:AutoFitWidth=\"0\" ss:Width=\"100\"/>");
            }

            xmlData.AppendLine("   <Row ss:AutoFitHeight=\"0\" ss:Height=\"26.25\" ss:StyleID=\"s23\">");
            foreach (DataColumn col in _dt.Columns)
            {
                xmlData.AppendLine("    <Cell ss:StyleID=\"s24\"><Data ss:Type=\"String\">" + _columnsTitle[col.ColumnName] + "</Data></Cell>");
            }
            xmlData.AppendLine("   </Row>");

            if (String.IsNullOrWhiteSpace(filter))
                filter = "";

            foreach (DataRow dr in _dt.Select(filter))
            {

                xmlData.AppendLine("   <Row>");
                foreach (DataColumn col in _dt.Columns)
                {
                    String type = "String";
                    String value = "";
                    String StyleID = "s28";
                    try
                    {
                        if (col.DataType == typeof(DateTime))
                        {
                            type = "DateTime";
                            value = (dr[col.ColumnName] != DBNull.Value && dr[col.ColumnName] != null ? ((DateTime)dr[col.ColumnName]).ToString("yyyy-MM-ddTHH:mm:ss.fff") : "");

                            //
                        }
                        else if ((col.DataType == typeof(Int16)) || (col.DataType == typeof(Int32)) || (col.DataType == typeof(Int64)))
                        {
                            type = "Number";
                            value = (dr[col.ColumnName] != DBNull.Value && dr[col.ColumnName] != null ? dr[col.ColumnName].ToString() : "");
                            StyleID = "s29";
                        }
                        else
                        {
                            type = "String";
                            value = (dr[col.ColumnName] != DBNull.Value && dr[col.ColumnName] != null ? dr[col.ColumnName].ToString() : "");
                        }

                    }
                    catch (Exception)
                    {
                        type = "String";
                        value = "";
                    }


                    if (value.IndexOf("=") == 0)
                    {
                        StyleID = "s29";
                        xmlData.AppendLine("    <Cell ss:StyleID=\"" + StyleID + "\" ss:Formula=\"" + value + "\"><Data ss:Type=\"" + type + "\"></Data></Cell>");
                    }
                    else
                    {
                        xmlData.AppendLine("    <Cell ss:StyleID=\"" + StyleID + "\"><Data ss:Type=\"" + type + "\">" + value + "</Data></Cell>");
                    }
                }
                xmlData.AppendLine("   </Row>");

            }

            xmlData.AppendLine("</Table>");
            xmlData.AppendLine("  <WorksheetOptions xmlns=\"urn:schemas-microsoft-com:office:excel\">");
            xmlData.AppendLine("   <PageSetup>");
            xmlData.AppendLine("    <Header x:Margin=\"0.49212598499999999\"/>");
            xmlData.AppendLine("    <Footer x:Margin=\"0.49212598499999999\"/>");
            xmlData.AppendLine("    <PageMargins x:Bottom=\"0.984251969\" x:Left=\"0.78740157499999996\"");
            xmlData.AppendLine("     x:Right=\"0.78740157499999996\" x:Top=\"0.984251969\"/>");
            xmlData.AppendLine("   </PageSetup>");
            xmlData.AppendLine("   <Print>");
            xmlData.AppendLine("    <ValidPrinterInfo/>");
            xmlData.AppendLine("    <PaperSizeIndex>9</PaperSizeIndex>");
            xmlData.AppendLine("    <HorizontalResolution>600</HorizontalResolution>");
            xmlData.AppendLine("    <VerticalResolution>600</VerticalResolution>");
            xmlData.AppendLine("   </Print>");
            xmlData.AppendLine("   <Selected/>");
            xmlData.AppendLine("   <Panes>");
            xmlData.AppendLine("    <Pane>");
            xmlData.AppendLine("     <Number>3</Number>");
            xmlData.AppendLine("     <ActiveRow>1</ActiveRow>");
            xmlData.AppendLine("     <ActiveCol>4</ActiveCol>");
            xmlData.AppendLine("    </Pane>");
            xmlData.AppendLine("   </Panes>");
            xmlData.AppendLine("   <ProtectObjects>False</ProtectObjects>");
            xmlData.AppendLine("   <ProtectScenarios>False</ProtectScenarios>");
            xmlData.AppendLine("  </WorksheetOptions>");
            xmlData.AppendLine(" </Worksheet>");
            xmlData.AppendLine("</Workbook>");

            return xmlData.ToString();
        }


        public void SaveToXML(String filename, String WorksheetName)
        {
            SaveToXML(filename, WorksheetName, "");
        }

        public void SateToTXT(String filename)
        {
            SateToTXT(filename, "");
        }

        public void SateToTXT(String filename, String filter)
        {
            String report = GetTXT(filter);
            
            File.WriteAllText(filename, report, Encoding.UTF8);
        }

        public String GetTXT()
        {
            return GetTXT("");
        }

        public String GetTXT(String filter)
        {
            //Verifica header das colunas
            foreach (DataColumn col in _dt.Columns)
            {
                if (!_columnsTitle.ContainsKey(col.ColumnName))
                    _columnsTitle.Add(col.ColumnName, col.ColumnName);
            }


            Dictionary<String, Int32> maxSize = new Dictionary<String, Int32>();

            foreach (DataColumn dc in _dt.Columns)
                maxSize.Add(dc.ColumnName, 0);

            foreach (DataColumn dc in _dt.Columns)
                if (maxSize[dc.ColumnName] < _columnsTitle[dc.ColumnName].Length + 3)
                    maxSize[dc.ColumnName] = _columnsTitle[dc.ColumnName].Length + 3;

            foreach (DataRow dr in _dt.Rows)
                foreach (DataColumn dc in _dt.Columns)
                    if (maxSize[dc.ColumnName] < (dr[dc.ColumnName] != DBNull.Value && dr[dc.ColumnName] != null ? dr[dc.ColumnName].ToString().Trim().Length + 3 : 0))
                        maxSize[dc.ColumnName] = (dr[dc.ColumnName] != DBNull.Value && dr[dc.ColumnName] != null ? dr[dc.ColumnName].ToString().Trim().Length + 3 : 0);

            StringBuilder report = new StringBuilder();

            List<String> lLine1 = new List<String>();
            List<String> lLine2 = new List<String>();
            foreach (DataColumn dc in _dt.Columns)
            {
                lLine1.Add(String.Format("{0,-" + maxSize[dc.ColumnName] + "}", _columnsTitle[dc.ColumnName]));
                lLine2.Add(new String("-".ToCharArray()[0], maxSize[dc.ColumnName]));
            }

            report.AppendLine(String.Join(" ", lLine1));
            report.AppendLine(String.Join(" ", lLine2));

            if (String.IsNullOrWhiteSpace(filter))
                filter = "";

            foreach (DataRow dr in _dt.Select(filter))
            {
                List<String> newLine = new List<String>();
                foreach (DataColumn dc in _dt.Columns)
                    if (dc.DataType == typeof(DateTime))
                        newLine.Add(String.Format("{0,-" + maxSize[dc.ColumnName] + "}", (dr[dc.ColumnName] != DBNull.Value && dr[dc.ColumnName] != null ? ((DateTime)dr[dc.ColumnName]).ToString("yyyy-MM-dd HH:mm:ss") : "")));
                    else
                        newLine.Add(String.Format("{0,-" + maxSize[dc.ColumnName] + "}", (dr[dc.ColumnName] != DBNull.Value && dr[dc.ColumnName] != null ? dr[dc.ColumnName].ToString().Trim() : "")));

                report.AppendLine(String.Join(" ", newLine));
            }

            return report.ToString();
        }


        public void SateToCSV(String filename)
        {
            SateToCSV(filename, "");
        }

        public void SateToCSV(String filename, String filter)
        {

            String report = GetCSV(filter);

            File.WriteAllText(filename, report, Encoding.UTF8);

        }

        public String GetCSV()
        {
            return GetCSV("");
        }

        public String GetCSV(String filter)
        {
            //Verifica header das colunas
            foreach (DataColumn col in _dt.Columns)
            {
                if (!_columnsTitle.ContainsKey(col.ColumnName))
                    _columnsTitle.Add(col.ColumnName, col.ColumnName);
            }

            StringBuilder report = new StringBuilder();

            List<String> lLine1 = new List<String>();
            foreach (DataColumn dc in _dt.Columns)
            {
                lLine1.Add(String.Format("{0}", _columnsTitle[dc.ColumnName]));
            }

            report.AppendLine(String.Join(",", lLine1));

            if (String.IsNullOrWhiteSpace(filter))
                filter = "";

            foreach (DataRow dr in _dt.Select(filter))
            {
                List<String> newLine = new List<String>();
                foreach (DataColumn dc in _dt.Columns)
                {
                    if (dc.DataType == typeof(DateTime))
                        newLine.Add(String.Format("{0}", (dr[dc.ColumnName] != DBNull.Value && dr[dc.ColumnName] != null ? ((DateTime)dr[dc.ColumnName]).ToString("yyyy-MM-dd HH:mm:ss") : "")));
                    else
                        newLine.Add(String.Format("{0}", (dr[dc.ColumnName] != DBNull.Value && dr[dc.ColumnName] != null ? dr[dc.ColumnName].ToString().Trim() : "")));
                }

                report.AppendLine(String.Join(",", newLine));
            }

            return report.ToString();

        }

        public void SaveToXML(String filename, String WorksheetName, String filter)
        {
            BinaryWriter writer = new BinaryWriter(File.Open(filename, FileMode.Create));
            writer.Write(Encoding.UTF8.GetBytes(GetXML(WorksheetName, filter)));
            writer.Flush();
            writer.BaseStream.Dispose();
            writer.Close();
        }

        public void SaveToFile(String filename)
        {
            FileInfo file = new FileInfo(filename);
            if (!file.Directory.Exists)
                file.Directory.Create();
            file = null;

            IFormatter formato = new BinaryFormatter();
            MemoryStream stream = new MemoryStream();
            formato.Serialize(stream, this);

            BinaryWriter writer = new BinaryWriter(File.Open(filename, FileMode.Create));
            writer.Write(stream.ToArray());
            writer.Flush();
            writer.BaseStream.Dispose();
            writer.Close();

        }

        public void LoadFromFile(String filename)
        {
            IFormatter formato = new BinaryFormatter();

            MemoryStream file = null;

            Int32 cnt = 0;

            FileInfo iFile = new FileInfo(filename);

            while ((cnt < 5) && (file == null))
            {
                try
                {
                    file = new MemoryStream(File.ReadAllBytes(filename));
                    //file = File.Open(filename, FileMode.Open, FileAccess.Read);
                }
                catch (Exception ex)
                {
                    cnt++;
                    if (cnt == 5)
                        throw ex;
                }
            }

            ReportBase item = (ReportBase)formato.Deserialize(file);
            file.Dispose();

            file.Dispose();
            file.Close();
            file = null;

            this._dt = item._dt;
            this._columnsTitle = item._columnsTitle;

        }

    }
}
