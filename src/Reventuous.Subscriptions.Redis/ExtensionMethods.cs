using System;
namespace Reventuous.Subscriptions.Redis
{
    public static class ExtensionMethods
    {
        /*
            Parses a Redis message id, and returns the date & time from the message id
            1635630312448-0 
        */
        public static DateTime ToDateAndTime(this string entryId)
        {
            var tokens = entryId.Split('-');
            var unixTimeStamp = double.Parse(tokens[0]);
            return (new DateTime()).AddMilliseconds(unixTimeStamp);
        }
    }
}