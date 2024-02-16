public class Store {
	public Dictionary<string, string> store = new Dictionary<string, string>();

	public string? Get(string key) {
		if (store.ContainsKey(key)) {
			return store[key];
		}
		return null;
	}

	public void Set(string key, string value) {
		store[key] = value;
	}
}

