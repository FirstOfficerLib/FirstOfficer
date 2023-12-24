using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FirstOfficer.Tests
{
    internal static class Extensions
    {
        public static string RandomString(this string str, int length)
        {
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 ";
            return new string(Enumerable.Repeat(chars, length)
                           .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static int RandomNumber(this int number)
        {
            var random = new Random();
            return random.Next(0, 1000);
        }


    }
}
