using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Resources;
using System.Threading;
using System.Globalization;
using IAM.GlobalDefs;

namespace IAM.Scheduler
{
    public enum ScheduleTtiggers
    {
        Dialy = 1,
        Weekly,
        Monthly,
        Annually
    }

    [Serializable()]
    public class Schedule: IDisposable
    {
        internal String trigger;
        internal String triggertime;
        
        [OptionalField]
        internal String startdate;

        [OptionalField]
        internal String repeat; //Minutes

        public DateTime StartDate { get { return DateTime.ParseExact(startdate, "yyyy-MM-dd", null); } set { startdate = value.ToString("yyyy-MM-dd"); } }

        //public DateTime StartDate { get { return DateTime.ParseExact(startdate, "yyyy-MM-dd", null); } set { startdate = value.ToString("yyyy-MM-dd"); } }

        public DateTime TriggerTime { get { return DateTime.ParseExact("1970-01-01 " + triggertime, "yyyy-MM-dd HH:mm:ss", null); } set { triggertime = value.ToString("HH:mm:ss"); } }

        public Int32 Repeat { get { return Int32.Parse((String.IsNullOrWhiteSpace(repeat) ? "0" : repeat)); } set { repeat = value.ToString(); } }

        public ScheduleTtiggers Trigger
        {
            get
            {
                switch (trigger.ToLower())
                {
                    case "monthly":
                        return ScheduleTtiggers.Monthly;
                        break;

                    case "annually":
                        return ScheduleTtiggers.Annually;
                        break;

                    case "weekly":
                        return ScheduleTtiggers.Weekly;
                        break;

                    default:
                        return ScheduleTtiggers.Dialy;
                        break;
                }
            }
            set { trigger = value.ToString().ToLower(); }
        }


        public void FromJsonString(String json)
        {
            Schedule item = null;
            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {

                DataContractJsonSerializer ser = new DataContractJsonSerializer(this.GetType());
                item = (Schedule)ser.ReadObject(ms);
            }

            if (item == null)
                return;

            this.trigger = item.trigger;
            this.startdate = item.startdate;
            this.triggertime = item.triggertime;
            this.repeat = item.repeat;
            if (this.repeat == null)
                this.repeat = "0";
        }

        public String ToJsonString()
        {
            String ret = "";
            DataContractJsonSerializer ser = new DataContractJsonSerializer(this.GetType());

            if (String.IsNullOrWhiteSpace(this.repeat))
                this.repeat = "0";

            using (MemoryStream ms = new MemoryStream())
            {
                ser.WriteObject(ms, this);
                ms.Flush();
                ret = Encoding.UTF8.GetString(ms.ToArray());
            }

            return ret;
        }

        public Dictionary<String, Object> ToJsonObject()
        {
            Dictionary<String, Object> ret = new Dictionary<string, object>();

            ret.Add("trigger", this.trigger);
            ret.Add("startdate", this.startdate);
            ret.Add("triggertime", this.triggertime);
            ret.Add("repeat", (String.IsNullOrWhiteSpace(this.repeat) ? "0" : this.repeat));

            return ret;
        }
        
        public override string ToString()
        {
            try
            {
                CultureInfo ci = Thread.CurrentThread.CurrentCulture;
                String ret = "";
                DateTime date = DateTime.ParseExact(startdate, "yyyy-MM-dd", null);

                ret += MessageResource.GetMessage(Trigger.ToString().ToLower()) + " ";
                switch (Trigger)
                {
                    case ScheduleTtiggers.Annually:
                        string tmp = date.ToString("d", ci).Replace(date.ToString("yyyy", ci), string.Empty);
                        char last = tmp[tmp.Length - 1];
                        char[] trimmer = char.IsDigit(last) ? new char[] { tmp[0] } : new char[] { last };
                        ret += tmp.Trim(trimmer) + " ";
                        break;

                    case ScheduleTtiggers.Monthly:
                        ret += MessageResource.GetMessage("day") + " " + date.Day + " ";
                        break;

                    case ScheduleTtiggers.Weekly:
                        ret += ci.DateTimeFormat.GetDayName(date.DayOfWeek).ToLower() + " ";
                        break;

                    default:
                        break;
                }
                ret += " " + MessageResource.GetMessage("in") + " ";
                ret += triggertime + (Repeat > 0 ? ", " + MessageResource.GetMessage("after_triggered") + " " + repeat + " " + MessageResource.GetMessage("minute") + "(s)" : "");

                return ret;
            }
            catch
            {
                return Trigger.ToString() + " in " + triggertime + (Repeat > 0 ? ", after triggered repeat every " + repeat + " minute(s)" : "");
            }
        }

        public DateTime CalcNext()
        {
            //Agenda a próxima execução
            DateTime calcNext = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, TriggerTime.Hour, TriggerTime.Minute, 0);
            DateTime nextExecute = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, 0);
            if (calcNext.CompareTo(DateTime.Now) == -1) //Ja passou a hora
            {
                switch (Trigger)
                {
                    case ScheduleTtiggers.Dialy:
                        calcNext = calcNext.AddDays(1);
                        break;

                    case ScheduleTtiggers.Weekly:
                        calcNext = calcNext.AddDays(7);
                        break;

                    case ScheduleTtiggers.Monthly:
                        calcNext = calcNext.AddMonths(1);
                        break;

                    case ScheduleTtiggers.Annually:
                        calcNext = calcNext.AddYears(1);
                        break;
                }
            }

            if (Repeat > 0)
            {
                if (nextExecute.AddMinutes(Repeat).CompareTo(calcNext) < 0)
                {
                    nextExecute = nextExecute.AddMinutes(Repeat);
                }
                else
                {
                    nextExecute = calcNext;
                }
            }
            else
                nextExecute = calcNext;

            return nextExecute;
        }

        public void Dispose()
        {
            this.trigger = null;
            this.startdate = null;
            this.triggertime = null;
            this.repeat = null;

        }
    }
}
