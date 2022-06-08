using API.Data;
using API.Models;
using API.TokenMng;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class PontajController : BaseApiController
    {

        public PontajController(PontajDbContext db, IJwtAuthenticationManager jwtAuthenticationManager, IHttpContextAccessor httpContextAccessor) :
            base(db, jwtAuthenticationManager, httpContextAccessor)
        {
        }

        private string getTimeAsString(TimeSpan ts)
        {
            string format = "";
            if (ts.Hours < 10)
                format += "0" + ts.Hours.ToString();
            else
                format += ts.Hours.ToString();

            if (ts.Minutes < 10)
                format += ":0" + ts.Minutes.ToString();
            else
                format += ":" + ts.Minutes.ToString();

            return format;
        }

        private TimeSpan getStringAsTime(string str)
        {
            return new TimeSpan(Int32.Parse(str.Substring(0, 2)), Int32.Parse(str.Substring(3, 2)), 0);
        }

        private TimeSpan getHoursWorkedInADay(DateTime date)
        {
            int dayId = db.EverySingleDays.Where(a => a.Date == date && a.UserId == userID).Select(a => a.DayId).FirstOrDefault();

            var fisaPostuluiTimeIntervals = db.FisaPostuluiDeBazas.Where(a => a.DayId == dayId).ToList();
            var plataCuOraTimeIntervals = db.PlataCuOras.Where(a => a.DayId == dayId).ToList();
            var projectsTimeIntervals = db.ProjectsTimeIntervals.Where(a => a.DayId == dayId).ToList();

            TimeSpan hoursWorked = new TimeSpan(0, 0, 0);

            foreach (var timeInterval in fisaPostuluiTimeIntervals)
                hoursWorked = hoursWorked.Add(timeInterval.EndTime.Subtract(timeInterval.StartTime));

            foreach (var timeInterval in plataCuOraTimeIntervals)
                hoursWorked = hoursWorked.Add(timeInterval.EndTime.Subtract(timeInterval.StartTime));

            foreach (var timeInterval in projectsTimeIntervals)
                hoursWorked = hoursWorked.Add(timeInterval.EndTime.Subtract(timeInterval.StartTime));

            return hoursWorked;
        }

        private List<string> GetProjectsList(string month, string year)
        {
            var days = db.EverySingleDays.Where(a =>
                                    a.Date.Month.ToString() == month && a.Date.Year.ToString() == year &&
                                    a.UserId == userID).Select(a => a.DayId).ToList();

            List<string> projectsList = new List<string>();

            var projects = db.ProjectsUsers.Where(a => a.UserId == userID).ToList();
            foreach (var project in projects)
            {   
                bool status = db.Projects.Where(a => a.ProjectId == project.ProjectId).Select(a => a.Status).FirstOrDefault();

                if (status)
                    projectsList.Add(db.Projects.Where(a => a.ProjectId == project.ProjectId).Select(a => a.ProjectName).FirstOrDefault());
                else
                {
                    foreach (var day in days)
                    {
                        var inactiveProject = db.ProjectsTimeIntervals.Where(a => a.ProjectId == project.ProjectId && a.DayId == day).FirstOrDefault();
                        if (inactiveProject != null)
                        {
                            projectsList.Add(db.Projects.Where(a => a.ProjectId == project.ProjectId).Select(a => a.ProjectName).FirstOrDefault());
                            break;
                        }
                    }
                }
            }

            return projectsList;
        }

        private List<Tuple<string, string, string>> GetProjectsHoursWorked(string year, string month)
        {
            var days = db.EverySingleDays.Where(a =>
                                    a.Date.Month.ToString() == month && a.Date.Year.ToString() == year &&
                                    a.UserId == userID).Select(a => a.DayId).ToList();


            List<Tuple<string, string, string>> hoursWorkedOnACertainDate = new List<Tuple<string, string, string>>();


            var projectsList = GetProjectsList(month, year);
            List<int> projectsId = new List<int>();
            foreach (var projectName in projectsList)
            {
                var id = db.Projects.Where(a => a.ProjectName == projectName).Select(a => a.ProjectId).FirstOrDefault();
                projectsId.Add(id);
            }


            foreach (var day in days)
            {
                foreach (var id in projectsId)
                {
                    var projectsTimeIntervals = db.ProjectsTimeIntervals.Where(a => a.DayId == day && a.ProjectId == id).ToList();

                    if (projectsTimeIntervals.Count == 0)
                        continue;

                    string date = db.EverySingleDays.Where(a =>
                                    a.DayId == day).Select(a => a.Date.Day).FirstOrDefault().ToString();

                    TimeSpan timeWorked = new TimeSpan(0, 0, 0);

                    foreach (var timeInterval in projectsTimeIntervals)
                    {
                        TimeSpan ts = timeInterval.EndTime.Subtract(timeInterval.StartTime);
                        timeWorked = timeWorked.Add(ts);
                    }

                    string timeWorkedAsString = getTimeAsString(timeWorked);

                    string projectName = db.Projects.Where(a => a.ProjectId == id).Select(a => a.ProjectName).FirstOrDefault();

                    Tuple<string, string, string> tuple = new Tuple<string, string, string>(date, projectName, timeWorkedAsString);
                    hoursWorkedOnACertainDate.Add(tuple);
                }
            }

            return hoursWorkedOnACertainDate;
        }

        private List<Tuple<string, string>> GetFisaPostuluiHoursWorked(string year, string month)
        {
            //se identifica id-urile zilelor din luna si anul selectat pentru user x
            var days = db.EverySingleDays.Where(a =>
                                    a.Date.Month.ToString() == month && a.Date.Year.ToString() == year &&
                                    a.UserId == userID).Select(a => a.DayId).ToList();

            // daca in baza de date nu exista zile din luna respectiva pt user x atunci se introduce pe toata luna
            // intervalul de lucru 7:30-15:30 la fisa postului
            if (days.Count == 0)
            {
                for (int i = 1; i <= DateTime.DaysInMonth(Int32.Parse(year), Int32.Parse(month)); i++)
                {
                    DateTime date = new DateTime(Int32.Parse(year), Int32.Parse(month), i);

                    if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                    {
                        EverySingleDay newDay = new EverySingleDay();
                        newDay.Date = date;
                        newDay.UserId = userID;

                        db.EverySingleDays.Add(newDay);
                        db.SaveChanges();
                        continue;
                    }

                    EverySingleDay day = new EverySingleDay();
                    day.Date = date;
                    day.UserId = userID;

                    db.EverySingleDays.Add(day);
                    db.SaveChanges();

                    int dayId = db.EverySingleDays.Where(a => a.Date == date && a.UserId == userID).Select(a => a.DayId).FirstOrDefault();

                    FisaPostuluiDeBaza obj = new FisaPostuluiDeBaza();
                    obj.DayId = dayId;
                    obj.StartTime = new TimeSpan(7, 30, 0);
                    obj.EndTime = new TimeSpan(15, 30, 0);

                    db.FisaPostuluiDeBazas.Add(obj);
                    db.SaveChanges();
                }

                days = db.EverySingleDays.Where(a =>
                                    a.Date.Month.ToString() == month && a.Date.Year.ToString() == year &&
                                    a.UserId == userID).Select(a => a.DayId).ToList();
            }

            List<Tuple<string, string>> hoursWorkedOnACertainDate = new List<Tuple<string, string>>();

            foreach (var day in days)
            {
                //se iau toate intervalele orare in care s-a lucrat la fisa postului de baza
                var fisaPostului = db.FisaPostuluiDeBazas.Where(a => a.DayId == day).ToList();

                if (fisaPostului.Count == 0)
                    continue;

                // se identifica ziua din data respectiva
                string date = db.EverySingleDays.Where(a =>
                                    a.DayId == day).Select(a => a.Date.Day).FirstOrDefault().ToString();

                TimeSpan timeWorked = new TimeSpan(0, 0, 0);

                //pentru fiecare interval orar din zi se calculeaza numarul de ore si minute lucrate si se adauga la total
                foreach (var item in fisaPostului)
                {
                    TimeSpan ts = item.EndTime.Subtract(item.StartTime);
                    timeWorked = timeWorked.Add(ts);
                }

                string TimeWorkedAsString = getTimeAsString(timeWorked);

                Tuple<string, string> tuple = new Tuple<string, string>(date, TimeWorkedAsString);
                hoursWorkedOnACertainDate.Add(tuple);
            }

            return hoursWorkedOnACertainDate;
        }

        private List<Tuple<string, string>> GetPlataCuOraHoursWorked(string year, string month)
        {
            //se identifica id-urile zilelor din luna si anul selectat pentru user x
            var days = db.EverySingleDays.Where(a =>
                                    a.Date.Month.ToString() == month && a.Date.Year.ToString() == year &&
                                    a.UserId == userID).Select(a => a.DayId).ToList();

            List<Tuple<string, string>> hoursWorkedOnACertainDate = new List<Tuple<string, string>>();

            foreach (var day in days)
            {
                //se iau toate intervalele orare in care s-a lucrat la plata cu ora
                var plataCuOra = db.PlataCuOras.Where(a => a.DayId == day).ToList();

                if (plataCuOra.Count == 0)
                    continue;

                // se identifica ziua din data respectiva
                string date = db.EverySingleDays.Where(a =>
                                    a.DayId == day).Select(a => a.Date.Day).FirstOrDefault().ToString();

                TimeSpan timeWorked = new TimeSpan(0, 0, 0);

                //pentru fiecare interval orar din zi se calculeaza numarul de ore si minute lucrate si se adauga la total
                foreach (var item in plataCuOra)
                {
                    TimeSpan ts = item.EndTime.Subtract(item.StartTime);
                    timeWorked = timeWorked.Add(ts);
                }

                string TimeWorkedAsString = getTimeAsString(timeWorked);

                Tuple<string, string> tuple = new Tuple<string, string>(date, TimeWorkedAsString);
                hoursWorkedOnACertainDate.Add(tuple);
            }

            return hoursWorkedOnACertainDate;
        }

        private List<Tuple<TimeSpan, TimeSpan>> GetAllTimeIntervalsFromADay(DateTime date)
        {
            int dayId = db.EverySingleDays.Where(a => a.Date == date && a.UserId == userID).Select(a => a.DayId).FirstOrDefault();

            var list1 = db.PlataCuOras.Where(a => a.DayId == dayId).ToList();
            var list2 = db.ProjectsTimeIntervals.Where(a => a.DayId == dayId).ToList();

            List<Tuple<TimeSpan, TimeSpan>> timeIntervals = new List<Tuple<TimeSpan, TimeSpan>>();

            foreach (var item in list1)
            {
                Tuple<TimeSpan, TimeSpan> tuple = new Tuple<TimeSpan, TimeSpan>(item.StartTime, item.EndTime);
                timeIntervals.Add(tuple);
            }

            foreach (var item in list2)
            {
                Tuple<TimeSpan, TimeSpan> tuple = new Tuple<TimeSpan, TimeSpan>(item.StartTime, item.EndTime);
                timeIntervals.Add(tuple);
            }

            return timeIntervals;
        }

        private void Reschedule(int dayId, TimeSpan lostHours, DateTime? recoveredFrom = null)
        {
            TimeSpan zero = new TimeSpan(0, 0, 0);

            DateTime date = db.EverySingleDays.Where(a => a.DayId == dayId).Select(a => a.Date).FirstOrDefault();
            var days = db.EverySingleDays.Where(a =>
                                a.Date.Year == date.Year && a.Date.Month == date.Month &&
                                a.Date.Day >= date.Day && a.UserId == userID).ToList();

            foreach (var day in days)
            {
                if (lostHours == zero)
                    break;

                if (day.Date.DayOfWeek == DayOfWeek.Saturday || day.Date.DayOfWeek == DayOfWeek.Sunday)
                    continue;

                TimeSpan start = new TimeSpan(15, 30, 0);

                TimeSpan hoursWorked = getHoursWorkedInADay(day.Date);
                TimeSpan twelve = new TimeSpan(12, 0, 0);
                if (hoursWorked == twelve)
                    continue;

                var timeIntervals = GetAllTimeIntervalsFromADay(day.Date);
                var fisaPostuluiTimeIntervals = db.FisaPostuluiDeBazas.Where(a => a.DayId == day.DayId && a.StartTime >= start).ToList();
                foreach (var timeInterval in fisaPostuluiTimeIntervals)
                    timeIntervals.Add(new Tuple<TimeSpan, TimeSpan>(timeInterval.StartTime, timeInterval.EndTime));

                bool removed = true;
                while (removed)
                {
                    if (timeIntervals.Count == 0)
                        break;

                    foreach (var timeInterval in timeIntervals)
                    {
                        if (timeInterval.Item1 < start)
                        {
                            timeIntervals.Remove(timeInterval);
                            removed = true;
                            break;
                        }
                        removed = false;

                    }
                }
                timeIntervals.Sort((x, y) => x.Item1.CompareTo(y.Item1));

                TimeSpan freeHours = twelve.Subtract(hoursWorked);
                if (lostHours >= freeHours)
                {
                    lostHours = lostHours.Subtract(freeHours);
                    TimeSpan hoursRecovered = new TimeSpan(0, 0, 0);

                    for (int i = 0; i < timeIntervals.Count; i++)
                    {
                        if (freeHours == zero)
                            break;

                        if (timeIntervals[i].Item1 == start)
                        {
                            start = timeIntervals[i].Item2;
                            continue;
                        }


                        FisaPostuluiDeBaza f = new FisaPostuluiDeBaza();
                        f.DayId = day.DayId;

                        if (recoveredFrom == null)
                            f.RecoveredFrom = date;
                        else
                            f.RecoveredFrom = recoveredFrom;

                        if (freeHours < timeIntervals[i].Item1.Subtract(start))
                        {
                            f.StartTime = start;
                            f.EndTime = start.Add(freeHours);
                        }
                        else
                        {
                            f.StartTime = start;
                            f.EndTime = timeIntervals[i].Item1;
                        }

                        start = timeIntervals[i].Item2;
                        hoursRecovered = hoursRecovered.Add(f.EndTime.Subtract(f.StartTime));

                        if (hoursRecovered < freeHours)
                        {
                            freeHours = freeHours.Subtract(hoursRecovered);
                        }
                        else
                        {
                            freeHours = zero;
                        }

                        db.FisaPostuluiDeBazas.Add(f);
                        db.SaveChanges();
                    }

                    if (freeHours > zero)
                    {
                        FisaPostuluiDeBaza f = new FisaPostuluiDeBaza();
                        f.DayId = day.DayId;

                        if (recoveredFrom == null)
                            f.RecoveredFrom = date;
                        else
                            f.RecoveredFrom = recoveredFrom;

                        f.StartTime = start;
                        f.EndTime = start.Add(freeHours);

                        db.FisaPostuluiDeBazas.Add(f);
                        db.SaveChanges();

                        freeHours = zero;
                    }
                }
                else
                {
                    TimeSpan hoursToRecover = lostHours;
                    TimeSpan hoursRecovered = new TimeSpan(0, 0, 0);

                    for (int i = 0; i < timeIntervals.Count; i++)
                    {
                        if (hoursToRecover == zero)
                            break;

                        if (timeIntervals[i].Item1 == start)
                        {
                            start = timeIntervals[i].Item2;
                            continue;
                        }


                        FisaPostuluiDeBaza f = new FisaPostuluiDeBaza();

                        if (recoveredFrom == null)
                            f.RecoveredFrom = date;
                        else
                            f.RecoveredFrom = recoveredFrom;

                        f.DayId = day.DayId;
                        if (hoursToRecover < timeIntervals[i].Item1.Subtract(start))
                        {
                            f.StartTime = start;
                            f.EndTime = start.Add(hoursToRecover);
                        }
                        else
                        {
                            f.StartTime = start;
                            f.EndTime = timeIntervals[i].Item1;
                        }

                        start = timeIntervals[i].Item2;
                        hoursRecovered = hoursRecovered.Add(f.EndTime.Subtract(f.StartTime));

                        if (hoursRecovered < hoursToRecover)
                        {
                            hoursToRecover = hoursToRecover.Subtract(hoursRecovered);
                        }
                        else
                        {
                            hoursToRecover = zero;
                        }

                        db.FisaPostuluiDeBazas.Add(f);
                        db.SaveChanges();
                    }

                    if (hoursToRecover > zero)
                    {
                        FisaPostuluiDeBaza f = new FisaPostuluiDeBaza();
                        f.DayId = day.DayId;

                        if (recoveredFrom == null)
                            f.RecoveredFrom = date;
                        else
                            f.RecoveredFrom = recoveredFrom;

                        f.StartTime = start;
                        f.EndTime = start.Add(hoursToRecover);

                        db.FisaPostuluiDeBazas.Add(f);
                        db.SaveChanges();

                        hoursToRecover = zero;
                    }

                    lostHours = zero;
                }
            }
        }

        private void EliminateConflictsAndRescheduleFisaPostului(PlataCuOra obj)
        {
            TimeSpan start = new TimeSpan(7, 30, 0);
            TimeSpan end = new TimeSpan(15, 30, 0);

            if (obj.StartTime.Hours == 8 || obj.StartTime.Hours == 10 || obj.StartTime.Hours == 12)
            {
                List<Tuple<TimeSpan, TimeSpan>> timeIntervals =
                    GetAllTimeIntervalsFromADay(db.EverySingleDays.Where(a => a.DayId == obj.DayId).Select(a => a.Date).FirstOrDefault());
                timeIntervals.Sort((x, y) => x.Item1.CompareTo(y.Item1));

                var fisaPostuluiTimeIntervals = db.FisaPostuluiDeBazas.Where(a =>
                                a.DayId == obj.DayId && a.EndTime <= end).ToList();

                foreach (var timeInterval in fisaPostuluiTimeIntervals)
                {
                    db.FisaPostuluiDeBazas.Remove(timeInterval);
                    db.SaveChanges();
                }

                if (timeIntervals[0].Item1.Hours == 8)
                {
                    FisaPostuluiDeBaza f1 = new FisaPostuluiDeBaza();
                    f1.DayId = obj.DayId;
                    f1.StartTime = start;
                    f1.EndTime = timeIntervals[0].Item1;

                    db.FisaPostuluiDeBazas.Add(f1);
                    db.SaveChanges();

                    if (timeIntervals.Count > 1 && timeIntervals[1].Item1.Hours == 10)
                    {
                        FisaPostuluiDeBaza f2 = new FisaPostuluiDeBaza();
                        f2.DayId = obj.DayId;
                        f2.StartTime = timeIntervals[0].Item2;
                        f2.EndTime = timeIntervals[1].Item1;

                        db.FisaPostuluiDeBazas.Add(f2);
                        db.SaveChanges();

                        if (timeIntervals.Count > 2 && timeIntervals[2].Item1.Hours == 12)
                        {
                            FisaPostuluiDeBaza f3 = new FisaPostuluiDeBaza();
                            f3.DayId = obj.DayId;
                            f3.StartTime = timeIntervals[1].Item2;
                            f3.EndTime = timeIntervals[2].Item1;

                            db.FisaPostuluiDeBazas.Add(f3);
                            db.SaveChanges();

                            FisaPostuluiDeBaza f4 = new FisaPostuluiDeBaza();
                            f4.DayId = obj.DayId;
                            f4.StartTime = timeIntervals[2].Item2;
                            f4.EndTime = end;
                            db.FisaPostuluiDeBazas.Add(f4);
                            db.SaveChanges();

                        }
                        else
                        {
                            FisaPostuluiDeBaza f3 = new FisaPostuluiDeBaza();
                            f3.DayId = obj.DayId;
                            f3.StartTime = timeIntervals[1].Item2;
                            f3.EndTime = end;

                            db.FisaPostuluiDeBazas.Add(f3);
                            db.SaveChanges();
                        }
                    }
                    else
                        if (timeIntervals.Count > 1 && timeIntervals[1].Item1.Hours == 12)
                    {
                        FisaPostuluiDeBaza f2 = new FisaPostuluiDeBaza();
                        f2.DayId = obj.DayId;
                        f2.StartTime = timeIntervals[0].Item2;
                        f2.EndTime = timeIntervals[1].Item1;

                        db.FisaPostuluiDeBazas.Add(f2);
                        db.SaveChanges();

                        FisaPostuluiDeBaza f3 = new FisaPostuluiDeBaza();
                        f3.DayId = obj.DayId;
                        f3.StartTime = timeIntervals[1].Item2;
                        f3.EndTime = end;

                        db.FisaPostuluiDeBazas.Add(f3);
                        db.SaveChanges();
                    }
                    else
                    {
                        FisaPostuluiDeBaza f2 = new FisaPostuluiDeBaza();
                        f2.DayId = obj.DayId;
                        f2.StartTime = timeIntervals[0].Item2;
                        f2.EndTime = end;

                        db.FisaPostuluiDeBazas.Add(f2);
                        db.SaveChanges();
                    }
                }
                else
                    if (timeIntervals[0].Item1.Hours == 10)
                {
                    FisaPostuluiDeBaza f1 = new FisaPostuluiDeBaza();
                    f1.DayId = obj.DayId;
                    f1.StartTime = start;
                    f1.EndTime = timeIntervals[0].Item1;

                    db.FisaPostuluiDeBazas.Add(f1);
                    db.SaveChanges();

                    if (timeIntervals.Count > 1 && timeIntervals[1].Item1.Hours == 12)
                    {
                        FisaPostuluiDeBaza f2 = new FisaPostuluiDeBaza();
                        f2.DayId = obj.DayId;
                        f2.StartTime = timeIntervals[0].Item2;
                        f2.EndTime = timeIntervals[1].Item1;

                        db.FisaPostuluiDeBazas.Add(f2);
                        db.SaveChanges();

                        FisaPostuluiDeBaza f3 = new FisaPostuluiDeBaza();
                        f3.DayId = obj.DayId;
                        f3.StartTime = timeIntervals[1].Item2;
                        f3.EndTime = end;

                        db.FisaPostuluiDeBazas.Add(f3);
                        db.SaveChanges();
                    }
                    else
                    {
                        FisaPostuluiDeBaza f2 = new FisaPostuluiDeBaza();
                        f2.DayId = obj.DayId;
                        f2.StartTime = timeIntervals[0].Item2;
                        f2.EndTime = end;

                        db.FisaPostuluiDeBazas.Add(f2);
                        db.SaveChanges();
                    }
                }
                else
                    if (timeIntervals[0].Item1.Hours == 12)
                {
                    FisaPostuluiDeBaza f1 = new FisaPostuluiDeBaza();
                    f1.DayId = obj.DayId;
                    f1.StartTime = start;
                    f1.EndTime = timeIntervals[0].Item1;

                    db.FisaPostuluiDeBazas.Add(f1);
                    db.SaveChanges();

                    FisaPostuluiDeBaza f2 = new FisaPostuluiDeBaza();
                    f2.DayId = obj.DayId;
                    f2.StartTime = timeIntervals[0].Item2;
                    f2.EndTime = end;

                    db.FisaPostuluiDeBazas.Add(f2);
                    db.SaveChanges();
                }
                Reschedule(obj.DayId, obj.EndTime.Subtract(obj.StartTime));
            }
            else
            {
                var fisaPostuluiTimeIntervals = db.FisaPostuluiDeBazas.Where(a => a.DayId == obj.DayId && a.StartTime >= end).ToList();
                if (fisaPostuluiTimeIntervals.Count == 0)
                    return;

                foreach (var timeInterval in fisaPostuluiTimeIntervals)
                {
                    db.FisaPostuluiDeBazas.Remove(timeInterval);
                    db.SaveChanges();
                    Reschedule(obj.DayId, timeInterval.EndTime.Subtract(timeInterval.StartTime), timeInterval.RecoveredFrom);
                }
            }
        }

        private void EliminateConflictsAndRescheduleFisaPostului(ProjectsTimeInterval obj)
        {
            TimeSpan end = new TimeSpan(15, 30, 0);
            DateTime date = db.EverySingleDays.Where(a => a.DayId == obj.DayId).Select(a => a.Date).FirstOrDefault();


            List<FisaPostuluiDeBaza> fisaPostuluiTimeIntervals;
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                return;
            else
            {
                fisaPostuluiTimeIntervals = db.FisaPostuluiDeBazas.Where(a => a.DayId == obj.DayId && a.StartTime >= end).ToList();
                if (fisaPostuluiTimeIntervals.Count == 0)
                    return;
            }

            foreach (var timeInterval in fisaPostuluiTimeIntervals)
            {
                db.FisaPostuluiDeBazas.Remove(timeInterval);
                db.SaveChanges();
                Reschedule(obj.DayId, timeInterval.EndTime.Subtract(timeInterval.StartTime), timeInterval.RecoveredFrom);
            }
        }

        private bool LessThan12Hours(DateTime date, TimeSpan start, TimeSpan end)
        {
            TimeSpan hoursWorked = new TimeSpan(0, 0, 0);
            int dayId = db.EverySingleDays.Where(a => a.Date == date && a.UserId == userID).Select(a => a.DayId).FirstOrDefault();

            if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
            {
                var timeIntervals = GetAllTimeIntervalsFromADay(date);
                bool removed = true;
                while (removed)
                {
                    if (timeIntervals.Count == 0)
                        break;

                    foreach (var timeInterval in timeIntervals)
                    {
                        if (timeInterval.Item1 < new TimeSpan(15, 30, 0))
                        {
                            timeIntervals.Remove(timeInterval);
                            removed = true;
                            break;
                        }
                        removed = false;

                    }
                }
                timeIntervals.Sort((x, y) => x.Item1.CompareTo(y.Item1));

                foreach (var timeInterval in timeIntervals)
                {
                    hoursWorked = hoursWorked.Add(timeInterval.Item2.Subtract(timeInterval.Item1));
                }

                hoursWorked = hoursWorked.Add(end.Subtract(start));

                if (hoursWorked <= new TimeSpan(4, 0, 0))
                    return true;
                else
                    return false;
            }
            else
            {
                var projectsTimeIntervals = db.ProjectsTimeIntervals.Where(a => a.DayId == dayId).ToList();

                foreach (var timeInterval in projectsTimeIntervals)
                {
                    hoursWorked = hoursWorked.Add(timeInterval.EndTime.Subtract(timeInterval.EndTime));
                }

                hoursWorked = hoursWorked.Add(end.Subtract(start));

                if (hoursWorked <= new TimeSpan(12, 0, 0))
                    return true;
                else
                    return false;
            }
        }

        private void deleteRescheduledFisaPostului(PlataCuOra obj)
        {
            if (obj.StartTime < new TimeSpan(15, 30, 0))
            {
                TimeSpan zero = new TimeSpan(0, 0, 0);

                FisaPostuluiDeBaza f = new FisaPostuluiDeBaza();
                f.DayId = obj.DayId;
                f.StartTime = obj.StartTime;
                f.EndTime = obj.EndTime;

                db.FisaPostuluiDeBazas.Add(f);
                db.SaveChanges();

                TimeSpan hoursToDelete = obj.EndTime.Subtract(obj.StartTime);

                DateTime date = db.EverySingleDays.Where(a => a.DayId == obj.DayId).Select(a => a.Date).FirstOrDefault();
                var days = db.EverySingleDays.Where(a =>
                                    a.Date.Year == date.Year && a.Date.Month == date.Month &&
                                    a.Date.Day >= date.Day && a.UserId == userID).ToList();

                foreach (var day in days)
                {
                    if (hoursToDelete == zero)
                        break;

                    if (day.Date.DayOfWeek == DayOfWeek.Saturday || day.Date.DayOfWeek == DayOfWeek.Sunday)
                        continue;

                    var fisaPostuluiTimeIntervals = db.FisaPostuluiDeBazas.Where(a => a.DayId == day.DayId && a.RecoveredFrom == date).ToList();
                    if (fisaPostuluiTimeIntervals.Count == 0)
                        continue;

                    foreach (var timeInterval in fisaPostuluiTimeIntervals)
                    {
                        if (timeInterval.EndTime.Subtract(timeInterval.StartTime) <= hoursToDelete)
                        {
                            hoursToDelete = hoursToDelete.Subtract(timeInterval.EndTime.Subtract(timeInterval.StartTime));
                            db.FisaPostuluiDeBazas.Remove(timeInterval);
                            db.SaveChanges();
                        }
                        else
                        {
                            db.FisaPostuluiDeBazas.Remove(timeInterval);
                            db.SaveChanges();

                            timeInterval.EndTime = timeInterval.StartTime
                                                    .Add(timeInterval.EndTime.Subtract(timeInterval.StartTime).Subtract(hoursToDelete));

                            db.FisaPostuluiDeBazas.Add(timeInterval);
                            db.SaveChanges();

                            hoursToDelete = zero;
                        }
                    }
                }
            }
        }

        [HttpPost("projects")]
        public IActionResult GetProjects([FromBody] PontajMessage message)
        {
            if (message.month == null || message.year == null)
                return BadRequest();

            var user = db.Credentials.Where(a => a.UserId == userID).FirstOrDefault();
            if (user == null)
                return new JsonResult("Invalid user!");

            var projectsList = GetProjectsList(message.month, message.year);

            return new JsonResult(projectsList);
        }

        [HttpPost("projectsHoursWorked")]
        public IActionResult GetProjectsHoursWorked([FromBody] PontajMessage message)
        {
            if (message.month == null || message.year == null)
                return BadRequest();

            List<Tuple<string, string, string>> hoursWorkedOnACertainDate =
                new List<Tuple<string, string, string>>(GetProjectsHoursWorked(message.year, message.month));


            return new JsonResult(hoursWorkedOnACertainDate);

        }

        [HttpPost("FisaPostuluiHoursWorked")]
        // pe baza selectiei unei luni si a unui an se va returna o lista cu numarul de ore lucrate
        // intr-o anumita zi din luna selectata pentru fisa postului
        public IActionResult GetFisaPostuluiHoursWorked([FromBody] PontajMessage message)
        {
            if (message.month == null || message.year == null)
                return BadRequest();

            List<Tuple<string, string>> hoursWorkedOnACertainDate =
                new List<Tuple<string, string>>(GetFisaPostuluiHoursWorked(message.year, message.month));

            // se returneaza ziua si nr de ore lucrate in fiecare zi;
            return new JsonResult(hoursWorkedOnACertainDate);
        }

        [HttpPost("PlataCuOraHoursWorked")]
        public IActionResult GetPlataCuOraHoursWorked([FromBody] PontajMessage message)
        {
            if (message.month == null || message.year == null)
                return BadRequest();

            List<Tuple<string, string>> hoursWorkedOnACertainDate =
                new List<Tuple<string, string>>(GetPlataCuOraHoursWorked(message.year, message.month));

            // se returneaza ziua si nr de ore lucrate in fiecare zi;
            return new JsonResult(hoursWorkedOnACertainDate);
        }

        [HttpPost("AddPlataCuOraTimeInterval")]
        public IActionResult AddPlataCuOraTimeInterval([FromBody] PontajPlataCuOraMessage message)
        {
            DateTime date = new DateTime(Int32.Parse(message.year), Int32.Parse(message.month), Int32.Parse(message.day));

            if (date.DayOfWeek == DayOfWeek.Sunday)
                return BadRequest("Nu se pot pot adauga ore la plata cu ora duminica.");

            if ((date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday) && message.studyProgram != "master")
                return BadRequest("Nu se pot pot adauga ore la plata cu ora sambata.(valabil pentru licenta)");

            int dayId = db.EverySingleDays.Where(a => a.Date == date && a.UserId == userID).Select(a => a.DayId).FirstOrDefault();

            PlataCuOra obj = new PlataCuOra();
            obj.DayId = dayId;

            TimeSpan startTime = getStringAsTime(message.startTime);
            TimeSpan endTime = getStringAsTime(message.endTime);
            obj.StartTime = startTime;
            obj.EndTime = endTime;

            obj.StudyGroup = message.studyGroup;
            obj.SubjectName = message.subjectName;
            if (message.formatType == "online")
            {
                obj.Type = true;
                obj.AppForOnline = message.appForOnline;
            }
            else
                obj.Type = false;

            if (message.studyProgram == "master")
                obj.StudyProgram = true;
            else
                obj.StudyProgram = false;

            var plataCuOraTimeIntervals = db.PlataCuOras.Where(a => a.DayId == dayId).ToList();
            foreach (var timeInterval in plataCuOraTimeIntervals)
            {
                if (obj.StartTime >= timeInterval.StartTime && obj.StartTime < timeInterval.EndTime ||
                    obj.EndTime > timeInterval.StartTime && obj.EndTime <= timeInterval.EndTime)
                    return BadRequest("Intervalul de timp introdus se suprapune cu un alt interval de timp alocat pentru plata cu ora.");
            }

            var projectsTimeIntervals = db.ProjectsTimeIntervals.Where(a => a.DayId == dayId).ToList();
            foreach (var timeInterval in projectsTimeIntervals)
            {
                if (obj.StartTime >= timeInterval.StartTime && obj.StartTime < timeInterval.EndTime ||
                    obj.EndTime >= timeInterval.StartTime && obj.EndTime < timeInterval.EndTime)
                    return BadRequest("Intervalul de timp introdus se suprapune cu un alt interval de timp alocat pentru proiecte de cercetare.");
            }

            db.PlataCuOras.Add(obj);
            db.SaveChanges();

            EliminateConflictsAndRescheduleFisaPostului(obj);

            return Ok(new
            {
                StausCode = 200,
                Message = "S-a adaugat cu succes!"
            });
        }

        [HttpPost("AddProjectTimeInterval")]
        public IActionResult AddProjectTimeInterval([FromBody] PontajProjectMessage message)
        {
            DateTime date = new DateTime(Int32.Parse(message.year), Int32.Parse(message.month), Int32.Parse(message.day));

            if (date.DayOfWeek == DayOfWeek.Sunday)
                return BadRequest("Nu se pot pot adauga ore la proiecte duminica.");

            int dayId = db.EverySingleDays.Where(a => a.Date == date && a.UserId == userID).Select(a => a.DayId).FirstOrDefault();

            ProjectsTimeInterval obj = new ProjectsTimeInterval();
            obj.DayId = dayId;
            TimeSpan startTime = getStringAsTime(message.startTime);
            TimeSpan endTime = getStringAsTime(message.endTime);
            obj.StartTime = startTime;
            obj.EndTime = endTime;

            if (obj.EndTime.Subtract(obj.StartTime).Minutes > 0)
                return BadRequest("Intervalul orar introdus trebuie sa echivaleze cu un numar fix de ore lucrate.");

            int projectId = db.Projects.Where(a => a.ProjectName == message.projectName).Select(a => a.ProjectId).FirstOrDefault();
            if (projectId == 0)
                return BadRequest("Proiectul nu exista!");
            obj.ProjectId = projectId;

            if (obj.StartTime < new TimeSpan(15, 30, 0) && date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                return BadRequest("Nu se poate introduce lucru la proicete mai devreme de 15:30 in timpul saptamanii.");

            if (obj.EndTime > new TimeSpan(20, 30, 0))
                return BadRequest("Nu se poate poate depasi ora 20:30 pentru lucru.");

            var plataCuOraTimeIntervals = db.PlataCuOras.Where(a => a.DayId == dayId).ToList();
            foreach (var timeInterval in plataCuOraTimeIntervals)
            {
                if (obj.StartTime >= timeInterval.StartTime && obj.StartTime < timeInterval.EndTime ||
                    obj.EndTime > timeInterval.StartTime && obj.EndTime <= timeInterval.EndTime)
                    return BadRequest("Intervalul de timp introdus se suprapune cu un alt interval de timp alocat pentru plata cu ora.");
            }

            var projectsTimeIntervals = db.ProjectsTimeIntervals.Where(a => a.DayId == dayId).ToList();
            foreach (var timeInterval in projectsTimeIntervals)
            {
                if (obj.StartTime >= timeInterval.StartTime && obj.StartTime < timeInterval.EndTime ||
                    obj.EndTime > timeInterval.StartTime && obj.EndTime <= timeInterval.EndTime)
                    return BadRequest("Intervalul de timp introdus se suprapune cu un alt interval de timp alocat pentru proiecte de cercetare.");
            }

            if (!LessThan12Hours(date, obj.StartTime, obj.EndTime))
                return BadRequest("Nu poti lucra atat. Depasesti 12 ore de munca.");

            db.ProjectsTimeIntervals.Add(obj);
            db.SaveChanges();

            EliminateConflictsAndRescheduleFisaPostului(obj);

            return Ok(new
            {
                StausCode = 200,
                Message = "S-a adaugat cu succes!"
            });
        }

        [HttpPost("GetPlataCuOraTimeIntervals")]
        public IActionResult GetPlataCuOraTimeIntervals([FromBody] PontajMessage message)
        {
            DateTime date = new DateTime(Int32.Parse(message.year), Int32.Parse(message.month), Int32.Parse(message.day));
            int dayId = db.EverySingleDays.Where(a => a.Date == date && a.UserId == userID).Select(a => a.DayId).FirstOrDefault();

            var timeIntervals = db.PlataCuOras.Where(a => a.DayId == dayId).ToList();

            List<PontajPlataCuOraMessage> response = new List<PontajPlataCuOraMessage>();

            foreach (var timeInterval in timeIntervals)
            {
                PontajPlataCuOraMessage newObj = new PontajPlataCuOraMessage();
                newObj.year = date.Year.ToString();
                newObj.month = date.Month.ToString();
                newObj.day = date.Day.ToString();

                newObj.startTime = getTimeAsString(timeInterval.StartTime);
                newObj.endTime = getTimeAsString(timeInterval.EndTime);

                newObj.subjectName = timeInterval.SubjectName;
                newObj.studyGroup = timeInterval.StudyGroup;

                if (timeInterval.Type == true)
                {
                    newObj.formatType = "online";
                    newObj.appForOnline = timeInterval.AppForOnline;
                }
                else
                    newObj.formatType = "fizic";

                if (timeInterval.StudyProgram == true)
                    newObj.studyProgram = "master";
                else
                    newObj.studyProgram = "licenta";

                response.Add(newObj);
            }

            return new JsonResult(response);
        }

        [HttpPost("GetProjectsTimeIntervals")]
        public IActionResult GetProjectsTimeIntervals([FromBody] PontajMessage message)
        {
            DateTime date = new DateTime(Int32.Parse(message.year), Int32.Parse(message.month), Int32.Parse(message.day));
            int dayId = db.EverySingleDays.Where(a => a.Date == date && a.UserId == userID).Select(a => a.DayId).FirstOrDefault();

            var timeIntervals = db.ProjectsTimeIntervals.Where(a => a.DayId == dayId).ToList();

            List<PontajProjectMessage> response = new List<PontajProjectMessage>();

            foreach (var timeInterval in timeIntervals)
            {
                PontajProjectMessage newObj = new PontajProjectMessage();
                newObj.year = date.Year.ToString();
                newObj.month = date.Month.ToString();
                newObj.day = date.Day.ToString();

                newObj.startTime = getTimeAsString(timeInterval.StartTime);
                newObj.endTime = getTimeAsString(timeInterval.EndTime);

                newObj.projectName = db.Projects.Where(a => a.ProjectId == timeInterval.ProjectId).Select(a => a.ProjectName).FirstOrDefault();

                response.Add(newObj);
            }

            return new JsonResult(response);
        }

        [HttpDelete("DeletePlataCuOraTimeInterval")]
        public IActionResult DeletePlataCuOraTimeInterval([FromBody] PontajPlataCuOraMessage message)
        {
            DateTime date = new DateTime(Int32.Parse(message.year), Int32.Parse(message.month), Int32.Parse(message.day));
            int dayId = db.EverySingleDays.Where(a => a.Date == date && a.UserId == userID).Select(a => a.DayId).FirstOrDefault();

            var timeInterval = db.PlataCuOras.Where(a => a.DayId == dayId && a.StartTime == getStringAsTime(message.startTime)).FirstOrDefault();

            db.PlataCuOras.Remove(timeInterval);

            deleteRescheduledFisaPostului(timeInterval);

            return Ok(new
            {
                StausCode = 200,
                Message = "S-a sters cu succes!"
            });
        }

        [HttpDelete("DeleteProjectTimeInterval")]
        public IActionResult DeleteProjectTimeInterval([FromBody] PontajProjectMessage message)
        {
            DateTime date = new DateTime(Int32.Parse(message.year), Int32.Parse(message.month), Int32.Parse(message.day));
            int dayId = db.EverySingleDays.Where(a => a.Date == date && a.UserId == userID).Select(a => a.DayId).FirstOrDefault();

            var timeInterval = db.ProjectsTimeIntervals.Where(a =>
                        a.DayId == dayId && a.StartTime == getStringAsTime(message.startTime)).FirstOrDefault();

            db.ProjectsTimeIntervals.Remove(timeInterval);

            return Ok(new
            {
                StausCode = 200,
                Message = "S-a sters cu succes!"
            });
        }

        //how to create middleware asp.net
    }
}
