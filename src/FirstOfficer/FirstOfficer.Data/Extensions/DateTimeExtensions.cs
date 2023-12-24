using System;

namespace FirstOfficer.Data.Extensions
{
    public static class DateTimeExtensions
    {
        public static long GetJavascriptEpoch(this DateTime value)
        {
            // Using DateTime(1970, 1, 1) gives us the start of UNIX epoch time
            TimeSpan elapsedTime = value - new DateTime(1970, 1, 1);

            // Convert the TimeSpan to milliseconds (since JavaScript uses milliseconds)
            return (long)elapsedTime.TotalMilliseconds;
        }



    }
}
