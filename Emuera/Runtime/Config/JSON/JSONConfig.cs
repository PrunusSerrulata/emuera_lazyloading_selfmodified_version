//新設したコンフィグ設定のロード、セーブ、公開を担当する。
using System.IO;
using System.Text.Json;


namespace MinorShift.Emuera.Runtime.Config.JSON;
static class JSONConfig
{
	public static JSONConfigData Data;

	const string _configFileName = "setting.json";
	static string _configFilePath = Program.ExeDir + _configFileName;

	internal static void HeadlessLoad()
	{
		if (!Program.HeadlessMode)
			throw new System.InvalidOperationException("Isolated JSON config loading is available only in headless mode");
		var configPath = Path.Combine(Program.ExeDir, _configFileName);
		Data = File.Exists(configPath)
			? JsonSerializer.Deserialize<JSONConfigData>(File.ReadAllText(configPath)) ?? new JSONConfigData()
			: new JSONConfigData();
	}

	public static void Load()
	{
		if (!File.Exists(_configFilePath))
		{
			var defaultData = new JSONConfigData();
			var defaultJson = JsonSerializer.Serialize(defaultData);
			File.WriteAllText(_configFilePath, defaultJson);
		}

		var json = File.ReadAllText(_configFilePath);

		Data = JsonSerializer.Deserialize<JSONConfigData>(json);
	}

	public static void Save()
	{
		var json = JsonSerializer.Serialize(Data);
		File.WriteAllText(_configFilePath, json);
	}
}
