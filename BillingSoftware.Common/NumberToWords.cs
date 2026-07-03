using System;

namespace BillingSoftware.Common;

public static class NumberToWords
{
    private static readonly string[] Units = { "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen" };
    private static readonly string[] Tens = { "", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };

    public static string ConvertAmount(decimal amount)
    {
        try
        {
            if (amount < 0)
            {
                return "Minus " + ConvertAmount(Math.Abs(amount));
            }

            long rupees = (long)Math.Floor(amount);
            int paise = (int)Math.Round((amount - rupees) * 100);

            string rupeesWord = ConvertToWords(rupees, false);
            if (string.IsNullOrEmpty(rupeesWord))
            {
                rupeesWord = "Rupees Zero";
            }
            else
            {
                rupeesWord = "Rupees " + rupeesWord;
            }

            string paiseWord = "";
            if (paise > 0)
            {
                paiseWord = " and " + ConvertToWords(paise, false) + " Paise";
            }

            return rupeesWord + paiseWord + " Only";
        }
        catch
        {
            return "Amount in words calculation error";
        }
    }

    private static string ConvertToWords(long number, bool includeAnd = false)
    {
        if (number == 0) return "";

        if (number < 20)
            return (includeAnd ? "and " : "") + Units[number];

        if (number < 100)
        {
            string tensPart = Tens[number / 10];
            string unitsPart = Units[number % 10];
            string separator = (number % 10 > 0) ? "-" : "";
            return (includeAnd ? "and " : "") + tensPart + separator + unitsPart;
        }

        if (number < 1000)
        {
            string hundredPart = Units[number / 100] + " Hundred";
            long remainder = number % 100;
            if (remainder > 0)
            {
                return hundredPart + " " + ConvertToWords(remainder, true);
            }
            return hundredPart;
        }

        if (number < 100000) // Thousand
        {
            string thousandPart = ConvertToWords(number / 1000) + " Thousand";
            long remainder = number % 1000;
            if (remainder > 0)
            {
                bool needAnd = remainder < 100;
                return thousandPart + " " + (needAnd ? "and " : "") + ConvertToWords(remainder, false);
            }
            return thousandPart;
        }

        if (number < 10000000) // Lakh (1,00,000)
        {
            string lakhPart = ConvertToWords(number / 100000) + " Lakh";
            long remainder = number % 100000;
            if (remainder > 0)
            {
                bool needAnd = remainder < 100;
                return lakhPart + " " + (needAnd ? "and " : "") + ConvertToWords(remainder, false);
            }
            return lakhPart;
        }

        // Crore (1,00,000,00)
        string crorePart = ConvertToWords(number / 10000000) + " Crore";
        long remainderCrore = number % 10000000;
        if (remainderCrore > 0)
        {
            bool needAnd = remainderCrore < 100;
            return crorePart + " " + (needAnd ? "and " : "") + ConvertToWords(remainderCrore, false);
        }
        return crorePart;
    }
}
