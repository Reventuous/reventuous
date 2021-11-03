namespace Subscriptions.Console
{
    public class Events
    {
        public record AccountCreated(string AccountNumber) {
            public override string ToString()
            {
                return $"AccountCreated AccountNumber={AccountNumber}";
            }
        }
        public record AccountCredited(double Amount) {
            public override string ToString()
            {
                return $"AccountCredited Amount={Amount}";
            }
        }
        public record AccountDebited(double Amount) {
            public override string ToString()
            {
                return $"AccountDebited Amount={Amount}";
            }
        }
        public record UserCreated(string UserId) {
            public override string ToString()
            {
                return $"UserCreated UserId={UserId}";
            }
        }

    }
}