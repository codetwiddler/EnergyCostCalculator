using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WattageCalculator
{
    public static class DecimalExtensions
    {
        //ex: ToDecimalPlaces(3) would give a decimal with three digits of precision
        public static decimal ToDecimalPlaces(this decimal value, int decimalPlaces)
        {
            return decimal.Round(value, decimalPlaces);
        }

        //attempting to acquire a value from a nullable that is null returns null
        public static decimal? ToDecimalPlaces(this decimal? value, int decimalPlaces)
        {
            if (!value.HasValue)
            {
                return null;
            }
            return decimal.Round(value.Value, decimalPlaces);
        }

        //ex: ToPercentWithDecimalPlaces(0) would give a whole number percentage
        public static decimal ToPercentWithDecimalPlaces(this decimal value, int decimalPlaces)
        {
            var percentage = value * 100;
            var roundedPercentage = decimal.Round(percentage, decimalPlaces);
            return roundedPercentage;
        }

        //attempting to acquire a value fropm a nullable that is null returns null
        public static decimal? ToPercentWithDecimalPlaces(this decimal? value, int decimalPlaces)
        {
            if (!value.HasValue)
            {
                return null;
            }

            var percentage = value.Value * 100;
            var roundedPercentage = decimal.Round(percentage, decimalPlaces);
            return roundedPercentage;
        }
    }
}
