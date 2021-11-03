using System;
using Reventuous;

namespace Reventuous.Tests.Model {
    public static class Events {
        public record AccountCreated(string AccountNumber);
        public record AmountLodged(decimal Amount);
        public record AmountWithdrawn(decimal Amount);
        
        public static void MapEvents() {
            TypeMap.AddType<AccountCreated>("AccountCreated");
            TypeMap.AddType<AmountLodged>("AmountLodged");
            TypeMap.AddType<AmountWithdrawn>("AmountWithdrawn");
        }
    }
}