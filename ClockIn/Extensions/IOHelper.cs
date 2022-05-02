using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace ClockIn.Extensions
{
    public class IOHelper
    {
        #region Excel
        private static List<object> GetListOfItems(DataTable table, int position)
        {
            List<object> list = new List<object>();
            foreach (var dataItem in table.AsEnumerable())
            {
                list.Add(dataItem.ItemArray[position]);
            }
            return list;
        }
        private static void PopulateCells(IXLRange xLRange, List<object> items)
        {
            int i = 0;
            foreach (var cell in xLRange.Cells())
            {
                cell.DataType = XLDataType.DateTime;
                cell.Value = items[i];
                i++;
            }
        }
        public static string ExportToExcel(DataTable table)
        {
            XLWorkbook xL = new XLWorkbook(@".\Templates\template.xlsx");
            var workSheet = xL.Worksheets.FirstOrDefault();
            var startTimes = GetListOfItems(table, 1);
            var endTimes = GetListOfItems(table, 2);
            var dates = GetListOfItems(table, 0);
            var startTimeRange = workSheet.Range("F12", $"F{11 + table.Rows.Count}");
            var endTimeRange = workSheet.Range("G12", $"G{11 + table.Rows.Count}");
            var datesRange = workSheet.Range("D12", $"D{11 + table.Rows.Count}");
            PopulateCells(startTimeRange, startTimes);
            PopulateCells(endTimeRange, endTimes);
            PopulateCells(datesRange, dates);
            var d = (DateTime)dates[0];
            string outPath = $"Reports\\{d.Year}_{d.ToString("MMMM")}_hours_report.xlsx";
            xL.SaveAs(outPath);
            return outPath;
        }
        #endregion
        public static string[] ReadFileContent()
        {
            var ofd = new OpenFileDialog();
            ofd.Filter = "Text|*.txt|All|*.*";
            ofd.InitialDirectory = ConfigurationManager.AppSettings["InitialDirectory"].ToString();
            var result = ofd.ShowDialog();
            if (result == DialogResult.OK)
            {

                var file = File.ReadAllText(ofd.FileName);
                string[] rows = file.Split('\n');
                return rows;
            }
            else
            {
                MainForm.log.Info(result);
                return null;
            }
        }
    }
}
