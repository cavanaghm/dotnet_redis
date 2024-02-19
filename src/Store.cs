public class Store
{
    public Dictionary<string, (string, DateTime?)> store = new Dictionary<string, (string, DateTime?)>();

    public string? Get(string key)
    {
        if (store.ContainsKey(key))
        {
            var val = store[key];
            if (val.Item2 != null && val.Item2 < DateTime.Now)
            {
                store.Remove(key);
                return null;
            }
            return val.Item1;
        }
        return null;
    }

    public void Set(SetCommands setCommands)
    {
        if (setCommands.ttl > 0)
        {
            store[setCommands.key] = (setCommands.value, DateTime.Now.AddMilliseconds(setCommands.ttl));
            return;
        }
        store[setCommands.key] = (setCommands.value, null);
    }
}
