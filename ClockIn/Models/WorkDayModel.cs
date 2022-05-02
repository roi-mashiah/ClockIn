using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClockIn.Models
{
    public class WorkDayModel
    {
        private readonly string _dateTimeStr;
        public DateTime Date { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public double OverTime { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public bool IsValid { get; set; }
        public WorkDayModel(string dateTimeStr)
        {
            _dateTimeStr = dateTimeStr.Trim(new char[] { '\r', '\n' });
            ParseRow();
        }

        private void ParseRow()
        {
            try
            {
                var arr = _dateTimeStr.Split(' ');
                string dateStr = arr[0];
                string[] hoursArr = arr[1].Split('-');
                Date = DateTime.ParseExact(s: dateStr, format: "dd.MM", null);
                Date.AddYears(Math.Abs(int.Parse(ConfigurationManager.AppSettings["ReferenceYear"]) - Date.Year));
                var startTime = DateTime.ParseExact(s: hoursArr[0], format: "HH:mm", null);
                StartTime = Date.AddHours(startTime.Hour).AddMinutes(startTime.Minute);
                var endTime = DateTime.ParseExact(s: hoursArr[1], format: "HH:mm", null);
                EndTime = Date.AddHours(endTime.Hour).AddMinutes(endTime.Minute);
                Duration = EndTime.TimeOfDay - StartTime.TimeOfDay;
                OverTime = Duration.TotalHours - 9;
                OverTime = OverTime > 0 ? OverTime : 0;
                DayOfWeek = Date.DayOfWeek;
                IsValid = true;
            }
            catch (Exception ex)
            {
                MainForm.log.Error(ex.Message);
                IsValid = false;
            }
        }
    }
}
