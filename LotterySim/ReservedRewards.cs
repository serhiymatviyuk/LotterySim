namespace LotterySim
{
    internal class ReserverRewardExpiration
    {
        public string RewardId { get; set; } = string.Empty;
        public DateTime ExpirationDate { get; set; }
    }

    internal class ReservedRewards
    {
        public List<ReserverRewardExpiration> UserRewards { get; set; } = new List<ReserverRewardExpiration>();

        public ReservedRewards(FakeDBRecord? claimRecord)
        {
            if (claimRecord != null)
            {
                var serialized = claimRecord.Values.GetValueOrDefault("reservation", "");
                var rewards = serialized.Split('|');

                foreach (string rewardInfo in rewards)
                {
                    var parts = rewardInfo.Split(':');

                    if (parts.Length > 1)
                    {
                        UserRewards.Add(new ReserverRewardExpiration
                        {
                            RewardId = parts[0],
                            ExpirationDate = new DateTime(long.Parse(parts[1]))
                        });
                    }
                }
            }
        }

        public string SerializeRewards()
        {
            return string.Join('|', UserRewards.Select(x => $"{x.RewardId}:{x.ExpirationDate.Ticks}").ToArray());
        }
    }
}
