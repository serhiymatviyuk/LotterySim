namespace LotterySim
{
    internal class FakeDBRecord
    {
        // All the data should be saved as string
        // so you don't need to consider whether the data is valid
        // anything as pre-setup value should be valid
        public Dictionary<string, string> Values = new Dictionary<string, string>();

        public bool TryGetNumericValue(string key, out float value)
        {
            value = 0;
            bool result = Values.TryGetValue(key, out string? text)
                && float.TryParse(text, out value);

            return result;
        }

        public float GetNumericValue(string key)
        {
            string? result = Values.GetValueOrDefault(key);
            return result != null ? float.Parse(Values.GetValueOrDefault(key, "")) : default;
        }
    }
}
