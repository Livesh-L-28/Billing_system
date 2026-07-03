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

            string rupeesWord = ConvertToWords(rupees);
            if (string.IsNullOrEmpty(rupeesWord))
            {
                rupeesWord = "Zero Rupees";
            }
            else
            {
                rupeesWord += " Rupees";
            }

            string paiseWord = "";
            if (paise > 0)
            {
                paiseWord = " and " + ConvertToWords(paise) + " Paise";
            }

            return rupeesWord + paiseWord + " Only";
        }
        catch
        {
            return "Amount in words calculation error";
        }
    }

    private static string ConvertToWords(long number)
    {
        if (number == 0) return "";

        if (number < 20)
            return Units[number];

        if (number < 100)
            return Tens[number / 10] + ((number % 10 > 0) ? " " + Units[number % 10] : "");

        if (number < 1000)
            return Units[number / 100] + " Hundred" + ((number % 100 > 0) ? " " + ConvertToWords(number % 100) : "");

        // Indian Numbering System: Lakhs and Crores
        if (number < 100000) // Thousand
            return ConvertToWords(number / 1000) + " Thousand" + ((number % 1000 > 0) ? " " + ConvertToWords(number % 1000) : "");

        if (number < 10000000) // Lakh (1,00,000)
            return ConvertToWords(number / 100000) + " Lakh" + ((number % 100000 > 0) ? " " + ConvertToWords(number % 100000) : "");

        // Crore (1,00,000,00)
        return ConvertToWords(number / 10000000) + " Crore" + ((number % 10000000 > 0) ? " " + ConvertToWords(number % 10000000) : "");
    }
}
