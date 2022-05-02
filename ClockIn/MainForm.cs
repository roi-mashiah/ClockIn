using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;
using System.IO;
using ClockIn.Models;
using System.Net.Mail;
using System.Net;
using ClockIn.Extensions;

namespace ClockIn
{
    public partial class MainForm : Form
    {
        #region Properties
        public DataTable timeTable = new DataTable();
        public BindingSource bS = new BindingSource();
        public static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        #endregion
        #region Initializers
        private void InitializeLabels()
        {
            totalHours.Text = "";
            totalDays.Text = "";
            totalOverTime.Text = "";
        }
        private void InitializeTimeTable()
        {
            var workDayModel = new WorkDayModel("04.07 09:30-18:30\r\n");
            var properties = workDayModel.GetType().GetProperties().ToList();
            for (int i = 0; i < properties.Count; ++i)
            {
                timeTable.Columns.Add(new DataColumn(properties[i].Name, properties[i].PropertyType));
            }
        }
        private void InitializeDGV()
        {
            bS.DataSource = timeTable;
            hoursDGV.DataSource = bS;
            hoursDGV.Columns["IsValid"].Visible = false;
            hoursDGV.AllowUserToAddRows = false;
            hoursDGV.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }
        #endregion
        #region Helper Methods
        private void UpdateRTB(MailMessage message)
        {
            var att = string.Join(" ,", message.Attachments.Select(a => a.Name).ToArray());
            string formattedStr = $"From: {message.From}\n" +
                                  $"To: {message.To}\n" +
                                  $"Subject: {message.Subject}\n" +
                                  $"Attachments: {att}\n" +
                                  $"Body: {message.Body}";
            richTextBox1.Text = formattedStr;
        }
        private void UpdateLabels()
        {
            TimeSpan totalHoursValue;
            totalHoursValue = (TimeSpan)timeTable.Compute("Sum(Duration)", null);
            double totalOverTimeValue;
            totalOverTimeValue = (double)timeTable.Compute("Sum(OverTime)", null);
            totalHours.Text = Math.Round(totalHoursValue.TotalHours, 2).ToString();
            totalDays.Text = timeTable.Rows.Count.ToString();
            totalOverTime.Text = Math.Round(totalOverTimeValue, 2).ToString();
        }
        #endregion
        public MainForm()
        {
            InitializeComponent();
            InitializeTimeTable();
            InitializeDGV();
            InitializeLabels();
            ConfigurationManager.AppSettings["ReportPath"] = "";
            UpdateRTB(MailHelper.GetMailMessage());
        }
        private void CheckForOutliers()
        {
            try
            {
                foreach (DataRow dataRow in timeTable.Rows)
                {
                    var ot = (double)dataRow["Overtime"];
                    var dow = (DayOfWeek)dataRow["DayOfWeek"];
                    if ((ot > 2 & !dow.Equals(DayOfWeek.Thursday)) || (ot > 1.5 & dow.Equals(DayOfWeek.Thursday)))
                    {
                        hoursDGV.Rows[timeTable.Rows.IndexOf(dataRow)].DefaultCellStyle.ForeColor = Color.Red;
                    }
                    else
                    {
                        hoursDGV.Rows[timeTable.Rows.IndexOf(dataRow)].DefaultCellStyle.ForeColor = Color.Black;
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                string[] rows = IOHelper.ReadFileContent();
                if (rows != null)
                {
                    for (int i = 0; i < rows.Length; ++i)
                    {
                        WorkDayModel workDay = new WorkDayModel(rows[i]);
                        DataRow r = timeTable.NewRow();
                        r.BeginEdit();
                        foreach (DataColumn c in timeTable.Columns)
                        {
                            string prop = c.ColumnName;
                            r[prop] = workDay.GetType().GetProperty(prop).GetValue(workDay);
                        }
                        r.EndEdit();
                        timeTable.Rows.Add(r);
                    }
                }
                UpdateLabels();
                CheckForOutliers();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                log.Error(ex.Message);
            }
        }
        private void clearTableToolStripMenuItem_Click(object sender, EventArgs e)
        {
            timeTable.Clear();
        }
        private void exportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (timeTable.Rows.Count > 0)
            {
                try
                {
                    string outPath = IOHelper.ExportToExcel(timeTable);
                    ConfigurationManager.AppSettings["ReportPath"] = outPath;
                    UpdateRTB(MailHelper.GetMailMessage());
                    MessageBox.Show($"Exported File to: {outPath}", "Exported to Excel", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    log.Error(ex.Message);
                }
            }
            else
            {
                MessageBox.Show($"Data has not been loaded!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }
        private void sendReport_Click(object sender, EventArgs e)
        {
            try
            {
                Forms.GetEmailData emailData = new Forms.GetEmailData();
                emailData.ShowDialog();
                var userName = ConfigurationManager.AppSettings["FromMail"];
                var password = emailData.Password;
                var credentials = new NetworkCredential(userName, password);
                var emailMessage = MailHelper.GetMailMessage();
                MailHelper.SendEmail(credentials, emailMessage);
                MessageBox.Show($"Sent Email Successfuly", "Sent Email", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                log.Error(ex.Message);
            }

        }

        private void FixBadRows(List<DataRow> badRows, double maxOt)
        {
            foreach (var badRow in badRows)
            {
                var remainder = badRow.Field<double>("OverTime") % maxOt;
                //var possibleRows = copyTable.AsEnumerable().ToList().Where(r => r.Field<double>("OverTime") + remainder <= maxOt).ToList();
                var possibleRows = timeTable.Select($"(Overtime + {remainder})<={maxOt}");
                var rowIndex = possibleRows.Select(selector => selector.Field<double>("OverTime"))
                .ToList().IndexOf(possibleRows.Max(r => r.Field<double>("OverTime")));
                var row = possibleRows[rowIndex];
                row["Overtime"] = (double)row["Overtime"] + remainder;
                row["EndTime"] = DateTime.Parse(row["EndTime"].ToString()).AddHours(remainder);
                row["Duration"] = DateTime.Parse(row["EndTime"].ToString()).TimeOfDay - DateTime.Parse(row["StartTime"].ToString()).TimeOfDay;

                badRow["Overtime"] = (double)badRow["Overtime"] - remainder;
                badRow["EndTime"] = DateTime.Parse(badRow["EndTime"].ToString()).AddHours(-remainder);
                badRow["Duration"] = DateTime.Parse(badRow["EndTime"].ToString()).TimeOfDay - DateTime.Parse(badRow["StartTime"].ToString()).TimeOfDay;
            }
        }

        private void analyzeHours_Click(object sender, EventArgs e)
        {
            var maxOtStoW = 2;
            var maxOtTh = 1.5;
            var badRowsStoW = timeTable.AsEnumerable().Where(t => t.Field<double>("OverTime") > maxOtStoW & !t.Field<DateTime>("Date").DayOfWeek.Equals(DayOfWeek.Thursday)).ToList();
            FixBadRows(badRowsStoW, maxOtStoW);
            var badRowsTh = timeTable.AsEnumerable().Where(t => t.Field<double>("OverTime") > maxOtTh & t.Field<DateTime>("Date").DayOfWeek.Equals(DayOfWeek.Thursday)).ToList();
            FixBadRows(badRowsTh, maxOtTh);
            InitializeDGV();
            CheckForOutliers();
        }
    }
}

