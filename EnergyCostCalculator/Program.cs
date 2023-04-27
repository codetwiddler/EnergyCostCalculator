using System;
using System.Buffers.Text;
using System.Net.NetworkInformation;
using System.Numerics;
using WattageCalculator;

class Program
{
    // Constants for cost per kilowatt-hour
    const decimal SUMMER_ON_PEAK_RATE  = 0.28m;
    const decimal WINTER_ON_PEAK_RATE  = 0.18m;
    const decimal SUMMER_MID_PEAK_RATE = 0.20m;
    const decimal WINTER_MID_PEAK_RATE = 0.15m;
    const decimal SUMMER_OFF_PEAK_RATE = 0.11m;
    const decimal WINTER_OFF_PEAK_RATE = 0.11m;

    public class TimeRange
    {
        public TimeSpan Start { get; set; }
        public TimeSpan End { get; set; }
    }

    // Time spans for each temporal category
    private static readonly TimeRange[] OffPeakRanges = new TimeRange[]
    {
        new TimeRange { Start = new TimeSpan(0, 0, 0), End = new TimeSpan(13, 0, 0) },
        new TimeRange { Start = new TimeSpan(19, 0, 0), End = new TimeSpan(24, 0, 0) }
    };

    private static readonly TimeRange[] MidPeakRanges = new TimeRange[]
    {
        new TimeRange { Start = new TimeSpan(13, 0, 0), End = new TimeSpan(15, 0, 0) }
    };

    private static readonly TimeRange[] OnPeakRanges = new TimeRange[]
    {
        new TimeRange { Start = new TimeSpan(15, 0, 0), End = new TimeSpan(19, 0, 0) }
    };

    static void Main()
    {
        Console.WriteLine("Enter wattage value: ");
        decimal wattage = Convert.ToDecimal(Console.ReadLine());

        // Get current date and time
        DateTime now = DateTime.Now;

        // Determine the current season (summer or winter) based on the current month
        bool isSummer = now.Month >= 6 && now.Month <= 9;

        // Calculate the monthly and annual costs
        decimal dailyCost = CalculateDailyCost(wattage, isSummer, now);
        decimal monthlyCost = CalculateMonthlyCost(wattage, isSummer);
        decimal annualCost = CalculateAnnualCost(wattage, isSummer, now.Month);

        Console.WriteLine($"Daily Cost: {dailyCost.ToDecimalPlaces(2):C}");
        Console.WriteLine($"Monthly cost: {monthlyCost.ToDecimalPlaces(2):C}");
        Console.WriteLine($"Annual cost: {annualCost.ToDecimalPlaces(2):C}");
    }

    public static decimal CalculateDailyCost(decimal wattage, bool isSummer, DateTime currentDate)
    {
        // Calculate energy consumption for each daily time range
        decimal offPeakEnergyConsumption = CalculateEnergyConsumption(wattage, isSummer,
            new TimeRange[]
            {
                new TimeRange { Start = new TimeSpan(0, 0, 0), End = new TimeSpan(7, 0, 0) },
                new TimeRange { Start = new TimeSpan(19, 0, 0), End = new TimeSpan(24, 0, 0) }
            },
            currentDate);

        decimal midPeakEnergyConsumption = CalculateEnergyConsumption(wattage, isSummer,
            new TimeRange[]
            {
                new TimeRange { Start = new TimeSpan(7, 0, 0), End = new TimeSpan(11, 0, 0) },
                new TimeRange { Start = new TimeSpan(17, 0, 0), End = new TimeSpan(19, 0, 0) }
            },
            currentDate);

        decimal onPeakEnergyConsumption = CalculateEnergyConsumption(wattage, isSummer,
            new TimeRange[]
            {
                new TimeRange { Start = new TimeSpan(11, 0, 0), End = new TimeSpan(17, 0, 0) }
            },
            currentDate);

        // Calculate the daily cost based on the season and energy consumption
        decimal currentSeasonOnPeakRate;
        decimal currentSeasonMidPeakRate;
        decimal currentSeasonOffPeakRate;

        if (currentDate.Month >= 6 && currentDate.Month <= 9)
        {
            // Summer season
            currentSeasonOffPeakRate = SUMMER_OFF_PEAK_RATE;
            currentSeasonMidPeakRate = SUMMER_MID_PEAK_RATE;
            currentSeasonOnPeakRate = SUMMER_ON_PEAK_RATE;
        }
        else
        {
            // Winter season
            currentSeasonOffPeakRate = WINTER_OFF_PEAK_RATE;
            currentSeasonMidPeakRate = WINTER_MID_PEAK_RATE;
            currentSeasonOnPeakRate = WINTER_ON_PEAK_RATE;
        }

        decimal dailyCost = (offPeakEnergyConsumption * currentSeasonOffPeakRate) +
                            (midPeakEnergyConsumption * currentSeasonMidPeakRate) +
                            (onPeakEnergyConsumption * currentSeasonOnPeakRate);

        return dailyCost;
    }

    public static decimal CalculateMonthlyCost(decimal wattage, bool isSummer)
    {
        DateTime currentDate = DateTime.Now;
        decimal totalCost = 0;
        decimal totalConsumption = 0;

        // Iterate through each day in the month
        for (int i = 1; i <= DateTime.DaysInMonth(currentDate.Year, currentDate.Month); i++)
        {
            bool isWeekend = currentDate.DayOfWeek == DayOfWeek.Saturday || currentDate.DayOfWeek == DayOfWeek.Sunday;

            decimal offPeakConsumption = CalculateEnergyConsumption(wattage, isSummer, OffPeakRanges, new DateTime(currentDate.Year, currentDate.Month, i));
            decimal offPeakCost = offPeakConsumption * (isSummer ? (isWeekend ? SUMMER_OFF_PEAK_RATE : SUMMER_ON_PEAK_RATE) : (isWeekend ? WINTER_OFF_PEAK_RATE : WINTER_ON_PEAK_RATE));

            totalConsumption += offPeakConsumption;

            decimal midPeakConsumption = CalculateEnergyConsumption(wattage, isSummer, MidPeakRanges, new DateTime(currentDate.Year, currentDate.Month, i));
            decimal midPeakCost = midPeakConsumption * (isSummer ? (isWeekend ? SUMMER_OFF_PEAK_RATE : SUMMER_MID_PEAK_RATE) : (isWeekend ? WINTER_OFF_PEAK_RATE : WINTER_MID_PEAK_RATE));

            totalConsumption += midPeakConsumption;

            decimal onPeakConsumption = CalculateEnergyConsumption(wattage, isSummer, OnPeakRanges, new DateTime(currentDate.Year, currentDate.Month, i));
            decimal onPeakCost = onPeakConsumption * (isSummer ? (isWeekend ? SUMMER_OFF_PEAK_RATE : SUMMER_ON_PEAK_RATE) : (isWeekend ? WINTER_OFF_PEAK_RATE : WINTER_ON_PEAK_RATE));

            totalConsumption += onPeakConsumption;

            // Calculate the total energy consumption and cost for all time ranges
            decimal dailyCost = offPeakCost + midPeakCost + onPeakCost;

            totalCost += dailyCost;
        }

        //debug value that should equal 72.0 (72KWh) if a 100W value is measured
        var totalEnergyConsumed = totalConsumption;

        return totalCost;
    }

    static decimal CalculateAnnualCost(decimal wattage, bool isSummer, int currentMonth)
    {
        // Get the total number of months remaining in the year, including the current month
        DateTime now = DateTime.Now;
        int monthsRemaining = 12 - currentMonth + 1;

        // Calculate the monthly cost and multiply by the number of months remaining in the year
        decimal monthlyCost = CalculateMonthlyCost(wattage, isSummer);
        decimal annualCost = monthlyCost * monthsRemaining;

        return annualCost;
    }

    public static decimal CalculateEnergyConsumption(decimal wattage, bool isSummer, TimeRange[] timeRanges, DateTime currentDate)
    {
        TimeSpan totalHours = TimeSpan.FromHours(0);

        foreach (var timeRange in timeRanges)
        {
            TimeSpan adjustedStartTime = timeRange.Start;
            TimeSpan adjustedEndTime = timeRange.End;

            if (isSummer)
            {
                // Adjust for daylight saving time in summer
                if (timeRange.Start == new TimeSpan(13, 0, 0)) // 1 PM to 3 PM
                {
                    DateTime daylightSavingStart = new DateTime(currentDate.Year, 3, 1);
                    int daysUntilSunday = ((int)DayOfWeek.Sunday - (int)daylightSavingStart.DayOfWeek + 7) % 7;
                    DateTime secondSunday = daylightSavingStart.AddDays(daysUntilSunday + 7);
                    DateTime daylightSavingEnd = secondSunday.AddDays(7);

                    if (currentDate >= daylightSavingStart && currentDate < daylightSavingEnd)
                    {
                        adjustedStartTime = timeRange.Start.Add(new TimeSpan(1, 0, 0)); // Add 1 hour
                        adjustedEndTime = timeRange.End.Add(new TimeSpan(1, 0, 0)); // Add 1 hour
                    }
                }
            }
            else
            {
                // Adjust for standard time in winter
                if (timeRange.End == new TimeSpan(19, 0, 0)) // 3 PM to 7 PM
                {
                    DateTime daylightSavingEnd = new DateTime(currentDate.Year, 11, 1);
                    int daysUntilSunday = ((int)DayOfWeek.Sunday - (int)daylightSavingEnd.DayOfWeek + 7) % 7;
                    DateTime firstSunday = daylightSavingEnd.AddDays(daysUntilSunday);
                    DateTime daylightSavingStart = firstSunday.AddDays(-7);

                    if (currentDate >= daylightSavingStart && currentDate < daylightSavingEnd)
                    {
                        adjustedStartTime = timeRange.Start.Subtract(new TimeSpan(1, 0, 0)); // Subtract 1 hour
                        adjustedEndTime = timeRange.End.Subtract(new TimeSpan(1, 0, 0)); // Subtract 1 hour
                    }
                }
            }


            totalHours = totalHours.Add(adjustedEndTime - adjustedStartTime); // Accumulate time differences
        }

        decimal energyConsumption = (decimal)totalHours.TotalHours * wattage / 1000;
        return energyConsumption;
    }
}