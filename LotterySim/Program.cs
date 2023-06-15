using LotterySim;

class Program
{
    public static void Main()
    {
        var db = new FakeDatabase();
        PreprareData(db);
        string? input;

        do
        {
            Console.WriteLine("Enter your command:");
            input = Console.ReadLine();
            if (!string.IsNullOrEmpty(input))
            {
                var userId = input.Substring(1);

                // Input A1 can do Attempt() for user 1
                // Input C2 can do Claim() for user 2
                if (input.StartsWith("A", StringComparison.OrdinalIgnoreCase))
                {
                    Attempt(db, userId);
                }
                else if (input.StartsWith("C"))
                {
                    Claim(db, userId);
                }
            }
        }
        while (input != "Q");
    }

    static void PreprareData(FakeDatabase db)
    {
        // Configurations
        var claimingTimeLimitRecord = new FakeDBRecord();
        claimingTimeLimitRecord.Values.Add("value", "30");
        db.Put(FakeDatabase.CLAIMING_TIME_TABLE, "", claimingTimeLimitRecord);

        var lotteryPriceRecord = new FakeDBRecord();
        lotteryPriceRecord.Values.Add("value", "10");
        db.Put(FakeDatabase.LOTTERY_PRICE_TABLE, "", lotteryPriceRecord);

        // Lottery prize levels
        var prizeLevel1 = new FakeDBRecord();
        prizeLevel1.Values.Add("id", "1");
        prizeLevel1.Values.Add("chance", "0.10");
        prizeLevel1.Values.Add("reward", "10");
        prizeLevel1.Values.Add("storage", "8");
        db.Put(FakeDatabase.PRIZE_LEVEL_TABLE, "1", prizeLevel1);

        var prizeLevel2 = new FakeDBRecord();
        prizeLevel2.Values.Add("id", "2");
        prizeLevel2.Values.Add("chance", "0.06");
        prizeLevel2.Values.Add("reward", "30");
        prizeLevel2.Values.Add("storage", "3");
        db.Put(FakeDatabase.PRIZE_LEVEL_TABLE, "2", prizeLevel2);

        var prizeLevel3 = new FakeDBRecord();
        prizeLevel3.Values.Add("id", "3");
        prizeLevel3.Values.Add("chance", "0.02");
        prizeLevel3.Values.Add("reward", "100");
        prizeLevel3.Values.Add("storage", "1");
        db.Put(FakeDatabase.PRIZE_LEVEL_TABLE, "3", prizeLevel3);

        // Users
        var user1Data = new FakeDBRecord();
        user1Data.Values.Add("id", "1");
        user1Data.Values.Add("name", "user1");
        user1Data.Values.Add("money", "500");
        db.Put(FakeDatabase.USER_TABLE, "1", user1Data);

        var user2Data = new FakeDBRecord();
        user2Data.Values.Add("id", "2");
        user2Data.Values.Add("name", "user2");
        user2Data.Values.Add("money", "100");
        db.Put(FakeDatabase.USER_TABLE, "2", user2Data);

        // Make more user if you think it's necessary
    }

    static void Attempt(FakeDatabase db, string userId)
    {
        float price = GetAttemptPrice(db);
        var user = GetUser(db, userId);

        if (user != null && DraftFunds(user, price))
        {
            var reward = GetReward(db);

            if (reward != null)
            {
                int secondsToClaim = GetClaimLimitSeconds(db);

                var remainingAmount = reward.GetNumericValue("storage") - 1;
                reward.Values["storage"] = remainingAmount.ToString();

                ReserveRewardForUser(db, userId, reward, secondsToClaim);

                Console.WriteLine($"You won {reward.GetNumericValue("reward")}! You can claim your reward in next {secondsToClaim} seconds");

                return;
            }

            Console.WriteLine("Nothing this time. Try again?");
        }

        ValidateRewards(db, DateTime.Now);
    }

    static void Claim(FakeDatabase db, string userId)
    {
        var currentDate = DateTime.Now;
        ValidateRewards(db, currentDate);

        FakeDBRecord? claimRecord = db.Get(FakeDatabase.USER_WINS_TABLE, userId);

        float priseAmount = 0;
        var toClaim = new ReservedRewards(claimRecord);
        foreach (var claiming in toClaim.UserRewards)
        {
            var rewardData = db.Get(FakeDatabase.PRIZE_LEVEL_TABLE, claiming.RewardId);
            if (rewardData != null)
            {
                priseAmount += rewardData.GetNumericValue("reward");
            }
        }

        if (priseAmount == 0)
        {
            Console.WriteLine("Nothing to claim.");
            return;
        }

        var user = GetUser(db, userId);
        if (user != null)
        {
            FillFunds(user, priseAmount);

            db.Remove(FakeDatabase.USER_WINS_TABLE, userId);

            Console.WriteLine($"You claim {priseAmount} Money to your balance!");
        }
    }

    /// <summary>
    /// Get reward expiration period in seconds.
    /// </summary>
    /// <param name="db">Database connection.</param>
    /// <returns></returns>
    private static int GetClaimLimitSeconds(FakeDatabase db)
    {
        var claimTimeRecord = db.Get(FakeDatabase.CLAIMING_TIME_TABLE, "");

        if (claimTimeRecord != null && claimTimeRecord.TryGetNumericValue("value", out float seconds))
            return (int)seconds;

        return default;
    }

    /// <summary>
    /// Get price of single attempt to win reward.
    /// </summary>
    /// <param name="db">Database connection.</param>
    /// <returns></returns>
    private static float GetAttemptPrice(FakeDatabase db)
    {
        var priceRecord = db.Get(FakeDatabase.LOTTERY_PRICE_TABLE, "");

        if (priceRecord != null && priceRecord.TryGetNumericValue("value", out float price))
            return price;

        return default;
    }

    /// <summary>
    /// Get user record by user id.
    /// </summary>
    /// <param name="db">Database connection.</param>
    /// <param name="userId">User id.</param>
    /// <returns></returns>
    private static FakeDBRecord? GetUser(FakeDatabase db, string userId)
    {
        return db.Get(FakeDatabase.USER_TABLE, userId);
    }

    /// <summary>
    /// Draft funds from user balance.
    /// </summary>
    /// <param name="user">User data.</param>
    /// <param name="price">Amount to draft.</param>
    /// <returns></returns>
    private static bool DraftFunds(FakeDBRecord user, float price)
    {
        if (user.TryGetNumericValue("money", out float funds))
        {
            var fundsLeft = funds - price;

            if (fundsLeft >= 0)
            {
                user.Values["money"] = fundsLeft.ToString();

                return true;
            }
        };

        Console.WriteLine("Not enough funds in your wallet");
        return false;
    }

    /// <summary>
    /// Add funds to user balance.
    /// </summary>
    /// <param name="user">User data.</param>
    /// <param name="price">Amount to draft.</param>
    /// <returns></returns>
    private static void FillFunds(FakeDBRecord user, float amount)
    {
        if (user.TryGetNumericValue("money", out float funds))
        {
            user.Values["money"] = (funds + amount).ToString();
        };
    }

    /// <summary>
    /// Calculate current reward.
    /// </summary>
    /// <param name="db">Database connection.</param>
    /// <returns></returns>
    private static FakeDBRecord? GetReward(FakeDatabase db)
    {
        FakeDBRecord? reward = default;

        var randomizer = new Random();
        var attemptResult = randomizer.NextSingle();

        var availableResults = db.GetCollection(FakeDatabase.PRIZE_LEVEL_TABLE);

        // Search for lowest chance win match
        foreach (var item in availableResults)
        {
            // is fit any available chance
            if (item.TryGetNumericValue("chance", out float winChance) && winChance >= attemptResult)
            {
                int availableRewards = (int)item.GetNumericValue("storage");
                if (availableRewards <= 0)
                    continue;

                // is current chance lower than previous
                if (reward == null || (reward.TryGetNumericValue("chance", out float current) && current > winChance))
                {
                    reward = item;
                }
            }
        }

        return reward;
    }

    /// <summary>
    /// Create record about reservation of reward.
    /// </summary>
    /// <param name="db">Database connection.</param>
    /// <param name="userId"></param>
    /// <param name="reward"></param>
    /// <param name="secondsToClaim"></param>
    private static void ReserveRewardForUser(FakeDatabase db, string userId, FakeDBRecord reward, int secondsToClaim)
    {
        bool creating = false;
        var reservationRecord = db.Get(FakeDatabase.USER_WINS_TABLE, userId);
        if (reservationRecord == null)
        {
            creating = true;
            reservationRecord = new FakeDBRecord();
            reservationRecord.Values.Add("reservation", "");
        }

        var rewardId = reward.Values.GetValueOrDefault("id");
        string formattedPart = $"{rewardId}:{DateTime.Now.AddSeconds(secondsToClaim).Ticks}";
        string fullReservation = reservationRecord.Values.GetValueOrDefault("reservation", "");
        reservationRecord.Values["reservation"] = fullReservation.Length > 0 ? $"{fullReservation}|{formattedPart}" : formattedPart;

        if (creating)
        {
            db.Put(FakeDatabase.USER_WINS_TABLE, userId, reservationRecord);
        }
        else
        {
            db.Update(FakeDatabase.USER_WINS_TABLE, userId, reservationRecord);
        }
    }

    /// <summary>
    /// Validate reserved rewards and remove expired records.
    /// </summary>
    /// <param name="db"></param>
    /// <param name="expirationDate"></param>
    private static void ValidateRewards(FakeDatabase db, DateTime expirationDate)
    {
        var allReservedRewards = db.GetCollection(FakeDatabase.USER_WINS_TABLE);

        foreach (var reservedReward in allReservedRewards)
        {
            var reward = new ReservedRewards(reservedReward);

            var updatedReservation = new List<ReserverRewardExpiration>();
            var cancelReservation = new List<ReserverRewardExpiration>();

            foreach (var item in reward.UserRewards)
            {
                if (item.ExpirationDate < expirationDate)
                {
                    cancelReservation.Add(item);
                }
                else
                {
                    updatedReservation.Add(item);
                }
            }

            reward.UserRewards = updatedReservation;
            reservedReward.Values["reservation"] = reward.SerializeRewards();

            foreach (var item in cancelReservation)
            {
                var rewardData = db.Get(FakeDatabase.PRIZE_LEVEL_TABLE, item.RewardId);
                if (rewardData != null)
                {
                    var rewardsInStorage = (int)rewardData.GetNumericValue("storage");
                    rewardsInStorage += 1;
                    rewardData.Values["storage"] = rewardsInStorage.ToString();
                }
            }
        }
    }
}
