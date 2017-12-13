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

namespace IAM.TimeACL
{
    public enum TimeAccessType
    {
        NotDefined = 0,
        Never,
        Always,
        SpecificTime
    }


    [Serializable()]
    public class TimeAccess: IDisposable
    {
        internal String type;
        internal String[] week_day;
        internal String start_time;
        internal String end_time;

        public DateTime StartTime { get { return DateTime.ParseExact("1970-01-01 " + start_time, "yyyy-MM-dd HH:mm:ss", null); } set { start_time = value.ToString("HH:mm:ss"); } }
        public DateTime EndTime { get { return DateTime.ParseExact("1970-01-01 " + end_time, "yyyy-MM-dd HH:mm:ss", null); } set { end_time = value.ToString("HH:mm:ss"); } }

        public TimeAccessType Type
        {
            get
            {
                switch (type.ToLower())
                {
                    case "never":
                        return TimeAccessType.Never;
                        break;

                    case "always":
                        return TimeAccessType.Always;
                        break;

                    case "specifictime":
                        return TimeAccessType.SpecificTime;
                        break;

                    default:
                        return TimeAccessType.NotDefined;
                        break;
                }
            }
            set { type = value.ToString().ToLower(); }
        }

        public List<DayOfWeek> WeekDay
        {
            get
            {
                List<DayOfWeek> wd = new List<DayOfWeek>();
                foreach (String w in week_day)
                {
                    switch (w.ToLower())
                    {
                        case "sunday":
                            wd.Add(DayOfWeek.Sunday);
                            break;

                        case "monday":
                            wd.Add(DayOfWeek.Monday);
                            break;

                        case "tuesday":
                            wd.Add(DayOfWeek.Tuesday);
                            break;

                        case "wednesday":
                            wd.Add(DayOfWeek.Wednesday);
                            break;

                        case "thursday":
                            wd.Add(DayOfWeek.Thursday);
                            break;

                        case "friday":
                            wd.Add(DayOfWeek.Friday);
                            break;

                        case "saturday":
                            wd.Add(DayOfWeek.Saturday);
                            break;
                            
                    }
                }
                return wd;
            }

            
            set
            {
                List<String> wd = new List<String>();
                foreach (DayOfWeek w in value)
                    wd.Add(w.ToString().ToLower());
                week_day = wd.ToArray();
            }

        }


        public List<String> WeekDay2
        {
            get
            {
                List<String> wd = new List<String>();
                wd.AddRange(week_day);
                return wd;
            }
        }


        public void FromString(String type, String start_time, String end_time, String week_day)
        {
            if (!String.IsNullOrWhiteSpace(type))
                this.type = type.ToLower();
            else
                this.type = TimeAccessType.NotDefined.ToString().ToLower();


            String tf = "{0:00}";

            try
            {
                String[] tm = start_time.ToString().Split(":".ToCharArray());

                this.StartTime = DateTime.ParseExact("1970-01-01 " + String.Format(tf, tm[0]) + ":" + String.Format(tf, tm[1]), "yyyy-MM-dd HH:mm", null);

            }
            catch
            {
                this.start_time = "00:00:00";
            }



            try
            {
                String[] tm = end_time.ToString().Split(":".ToCharArray());

                this.EndTime = DateTime.ParseExact("1970-01-01 " + String.Format(tf, tm[0]) + ":" + String.Format(tf, tm[1]), "yyyy-MM-dd HH:mm", null);

            }
            catch
            {
                this.end_time = "00:00:00";
            }

            List<DayOfWeek> wd = new List<DayOfWeek>();
            if (!String.IsNullOrWhiteSpace(week_day))
                foreach (String w in week_day.Split(",".ToCharArray()))
                {
                    switch (w.ToLower())
                    {
                        case "sunday":
                            wd.Add(DayOfWeek.Sunday);
                            break;

                        case "monday":
                            wd.Add(DayOfWeek.Monday);
                            break;

                        case "tuesday":
                            wd.Add(DayOfWeek.Tuesday);
                            break;

                        case "wednesday":
                            wd.Add(DayOfWeek.Wednesday);
                            break;

                        case "thursday":
                            wd.Add(DayOfWeek.Thursday);
                            break;

                        case "friday":
                            wd.Add(DayOfWeek.Friday);
                            break;

                        case "saturday":
                            wd.Add(DayOfWeek.Saturday);
                            break;

                        case "":
                            break;

                        default:
                            throw new Exception("Invalid week day '" + w + "'");
                            break;
                    }
                }
            this.WeekDay = wd;
        }

        public void FromJsonString(String json)
        {
            TimeAccess item = null;
            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {

                DataContractJsonSerializer ser = new DataContractJsonSerializer(this.GetType());
                item = (TimeAccess)ser.ReadObject(ms);
            }

            if (item == null)
                return;

            this.type = item.type;
            this.week_day = item.week_day;
            this.start_time = item.start_time;
            this.end_time = item.end_time;
        }

        public String ToJsonString()
        {
            String ret = "";
            DataContractJsonSerializer ser = new DataContractJsonSerializer(this.GetType());

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

            ret.Add("type", this.type);
            ret.Add("week_day", this.week_day);
            ret.Add("start_time", this.start_time);
            ret.Add("end_time", this.end_time);

            return ret;
        }

        public override string ToString()
        {
            String ret = "";

            switch (Type)
            {   
                case TimeAccessType.Never:
                    ret += MessageResource.GetMessage("never");
                    break;

                case TimeAccessType.Always:
                    ret += MessageResource.GetMessage("always");
                    break;

                case TimeAccessType.SpecificTime:
                    CultureInfo ci = Thread.CurrentThread.CurrentCulture;
                    
                    ret += StartTime.ToString("HH:mm") + " - " + EndTime.ToString("HH:mm") + " " + MessageResource.GetMessage("in") + " ";
                    List<String> week = new List<string>();
                    foreach (DayOfWeek w in WeekDay)
                        week.Add(ci.DateTimeFormat.GetDayName(w));
                    ret += String.Join(", ", week);
                    break;

                default:
                    ret += MessageResource.GetMessage("not_defined");
                    break;

            }

            return ret;
        }

        public Boolean BetweenTimes(DateTime time)
        {
            if (this.WeekDay.Exists(d => (d.Equals(time.DayOfWeek)))) //Contem o dia da semana atual
            {
                Int64 startMin = (this.StartTime.Hour * 60) + this.StartTime.Minute;
                Int64 endMin = (this.EndTime.Hour * 60) + this.EndTime.Minute;
                Int64 calcMin = (time.Hour * 60) + time.Minute;

                //Verifica o horário atual
                return ((calcMin >= startMin) && (calcMin <= endMin));
            }
            else
            {
                return false;
            }
        }

        public void Dispose()
        {
            this.type = null;
            this.week_day = null;
            this.start_time = null;
            this.end_time = null;
        }
    }
}
