namespace LotterySim
{
    internal class FakeDatabase
    {
        public static readonly string CLAIMING_TIME_TABLE = "Claiming_Time_Limit";
        public static readonly string LOTTERY_PRICE_TABLE = "Lottery_Price";
        public static readonly string PRIZE_LEVEL_TABLE = "Prize_Level";
        public static readonly string USER_TABLE = "User";
        public static readonly string USER_WINS_TABLE = "User_Wins";

        readonly Dictionary<(string, string), FakeDBRecord> Records = new Dictionary<(string, string), FakeDBRecord>();

        public FakeDBRecord? Get(string key1, string key2)
        {
            var recordExist = Records.TryGetValue((key1, key2), out var match);
            if (recordExist)
            {
                return match;
            }
            else
            {
                return null;
            }
        }

        public bool Put(string key1, string key2, FakeDBRecord record)
        {
            return Records.TryAdd((key1, key2), record);
        }

        //public IEnumerable<FakeDBRecord> Query()
        //{
        // // Implement on your need
        //}

        // Implement anything you need

        public IEnumerable<FakeDBRecord> GetCollection(string collectionName)
        {
            return Records
                .Where(x => x.Key.Item1 == collectionName)
                .Select(x => x.Value)
                .ToList();
        }

        public void Update(string key1, string key2, FakeDBRecord record)
        {
            if (Records.ContainsKey((key1, key2)))
            {
                Records[(key1, key2)] = record;
            }
        }

        public void Remove(string key1, string key2)
        {
            if (Records.ContainsKey((key1, key2)))
            {
                Records.Remove((key1, key2));
            }
        }
    }
}
