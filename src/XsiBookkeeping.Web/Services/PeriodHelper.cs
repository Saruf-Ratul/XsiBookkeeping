using System;

namespace XsiBookkeeping.Web.Services
{
    public struct YearMonth
    {
        public int Year { get; set; }
        public int Month { get; set; }
    }

    public static class PeriodHelper
    {
        public static readonly string[] MonthFull =
        {
            "January", "February", "March", "April", "May", "June",
            "July", "August", "September", "October", "November", "December"
        };

        public static YearMonth GetToday()
        {
            var d = DateTime.Today;
            return new YearMonth { Year = d.Year, Month = d.Month - 1 };
        }

        public static YearMonth GetLastMonth()
        {
            var d = DateTime.Today;
            var m = d.Month - 2;
            var y = d.Year;
            if (m < 0)
            {
                m += 12;
                y--;
            }
            return new YearMonth { Year = y, Month = m };
        }

        public static string ToMonthKey(int year, int monthZeroBased)
        {
            return $"{year}-{monthZeroBased + 1:D2}";
        }

        public static YearMonth ParseMonthKey(string monthKey)
        {
            var parts = monthKey.Split('-');
            return new YearMonth
            {
                Year = int.Parse(parts[0]),
                Month = int.Parse(parts[1]) - 1
            };
        }

        public static YearMonth GetPreviousPeriod(int year, int monthZeroBased)
        {
            var m = monthZeroBased - 1;
            var y = year;
            if (m < 0)
            {
                m = 11;
                y--;
            }
            return new YearMonth { Year = y, Month = m };
        }

        public static string NextStatus(string current)
        {
            if (string.IsNullOrEmpty(current) || current == "none")
                return "in-progress";
            if (current == "in-progress")
                return "done";
            return "none";
        }

        public static string DisplayStatus(string status)
        {
            if (status == "done") return "done";
            if (status == "in-progress") return "progress";
            return "none";
        }

        public static string FormatTime(DateTime iso)
        {
            return iso.ToLocalTime().ToString("MMM d") + " · " + iso.ToLocalTime().ToString("h:mm tt");
        }

        public static string AuthorColor(string name)
        {
            var colors = new[] { "#c2410c", "#0369a1", "#15803d", "#7e22ce", "#be185d", "#b45309" };
            var h = 0;
            foreach (var c in name ?? "")
                h = (h * 31 + c) % colors.Length;
            return colors[h];
        }

        public static string CompletionKey(long companyId, long accountId, string monthKey)
        {
            return $"{companyId}-{accountId}-{monthKey}";
        }
    }
}
