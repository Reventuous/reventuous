using System;
using Reventuous;
using static Subscriptions.Console.Events;

namespace Subscriptions.Console
{
    internal static class EventMapping
    {
        public static void MapEventTypes()
        {
            TypeMap.AddType<AccountCreated>("AccountCreated");
            TypeMap.AddType<AccountCredited>("AccountCredited");
            TypeMap.AddType<AccountDebited>("AccountDebited");
            TypeMap.AddType<UserCreated>("UserCreated");
        }
    }
}