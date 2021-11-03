namespace Reventuous {
    public record AppendEventsResult(string LastEventId, long NextExpectedVersion) {
        public static AppendEventsResult NoOp = new("0", -1);
    }
}