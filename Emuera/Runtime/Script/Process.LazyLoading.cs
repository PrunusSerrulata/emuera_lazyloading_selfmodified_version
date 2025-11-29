using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using MinorShift.Emuera.Runtime.Config;
using MinorShift.Emuera.Runtime.Script.Loader;
using MinorShift.Emuera.Runtime.Script.Statements;
using trsl = MinorShift.Emuera.Runtime.Utils.EvilMask.Lang.SystemLine;

namespace MinorShift.Emuera.GameProc;

internal sealed partial class Process
{
	//Main Working Sets
	private Dictionary<string, List<string>> LazyLoadingTable { get; } = Config.IgnoreCase ? new(StringComparer.OrdinalIgnoreCase) : new();
	private Dictionary<string, long> LazyLoadingFilesTable { get; } = Config.IgnoreCase ? new(StringComparer.OrdinalIgnoreCase) : new();
	public HashSet<string> LazyLoadingFiles { get; } = new();

	//For changes in files
	public readonly HashSet<string> DeletedFiles = new();
	public readonly HashSet<string> ChangedFiles = new();

	//Paths
	static readonly string LazyLoadingDataFilePath = Path.Join(Program.WorkingDir, "lazyloading.dat");
	static readonly string LazyLoadingFilesFilePath = Path.Join(Program.WorkingDir, "lazyloadingfiles.dat");
	static readonly string LazyLoadingConfigFilePath = Path.Join(Program.WorkingDir, "lazyloading.cfg");
	
	public enum LazyStatus
	{
		Disabled,
		NoLazy,
		BuildTable,
		Loaded,
		Error,
		UpdateTable,
	}

	public LazyStatus LazyCurrentLazyStatus = LazyStatus.Disabled;

	private const char Separator = '\t';

	public bool TryLazyLoadErb(string functionName)
	{
		if (!LazyLoadingTable.TryGetValue(functionName, out List<string> value))
		{
			return false;
		}

		var loader = new ErbLoader(console, exm, this);
		if (loader.LoadErbList(value, labelDic).GetAwaiter().GetResult())
		{
			if (Program.AnalysisMode)
			{
				foreach (var str in value)
					console.PrintSystemLine(string.Format(trsl.LazyLoadingDebugLoadingFile.Text, str));
			}

			//For updating the table i need this
			//LazyLoadingTable.Remove(functionName); // 로딩이 끝나면 해당 테이블 값은 필요가 없음.
			return true;
		}

		console.PrintSystemLine(string.Format(trsl.LazyLoadingErbFileNotFound.Text, functionName));
		return false;
	}

	private List<string> LoadLazyLoadingFolders()
	{
		var oldChar = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? '\\' : '/';
		char newChar = Path.DirectorySeparatorChar;

		if (!File.Exists(LazyLoadingConfigFilePath))
		{
			console.PrintSystemLine(trsl.LazyLoadingNoConfigFile.Text);
			return null; // 설정파일이 없으므로 일반 풀로딩을 해야 함.
		}

		List<string> ret = new();
		string line;
		try
		{
			using var reader = new StreamReader(LazyLoadingConfigFilePath, Encoding.UTF8);
			while ((line = reader.ReadLine()) != null)
				ret.Add(line.Trim().Replace(oldChar, newChar));
		}
		catch (Exception e)
		{
			console.PrintSystemLine(string.Format(trsl.LazyLoadingConfigError.Text, e.Message));
			return null;
		}

		return ret;
	}


	public void LoadLazyLoadingTable(List<KeyValuePair<string, string>> erbFiles)
	{
		if (!File.Exists(LazyLoadingConfigFilePath))
			return;
		if (!File.Exists(LazyLoadingDataFilePath) || !File.Exists(LazyLoadingFilesFilePath))
		{
			LazyCurrentLazyStatus = LazyStatus.BuildTable;
			return; // 설정파일이 없으므로 일반 풀로딩을 해야 함.
		}
		
		try
		{
			using var reader = new StreamReader(LazyLoadingDataFilePath, Encoding.UTF8);
			using var metareader = new StreamReader(LazyLoadingFilesFilePath, Encoding.UTF8);

			var files = GetLazyFiles(erbFiles);

			string line;
			string[] tokens;

			while ((line = metareader.ReadLine()) != null)
			{
				tokens = line.Split(Separator, 2);

				var name = tokens[0];
				long fLastWrite;
				try
				{
					//Try to parse the Windows File Time
					fLastWrite = long.Parse(tokens[1]);
				}
				catch (Exception)
				{
					//This would probably happen on old setups
					//or the file was edited manually
					LazyCurrentLazyStatus = LazyStatus.BuildTable;
					return;
				}
				
				var path = ErbPath(name);

				if (File.Exists(path))
				{
					if (File.GetLastWriteTime(path).ToFileTimeUtc() != fLastWrite)
					{
						ChangedFiles.Add(name);
					}
					else
					{
						LazyLoadingFilesTable.Add(name, fLastWrite);
					}
				}
				else
				{
					DeletedFiles.Add(name);
				}
			}

			files.ExceptWith(LazyLoadingFilesTable.Keys);
			ChangedFiles.UnionWith(files);

			while ((line = reader.ReadLine()) != null)
			{
				tokens = line.Split(Separator, 2);

				if (ChangedFiles.Contains(tokens[1]) || DeletedFiles.Contains(tokens[1]))
					continue;

				if (!LazyLoadingTable.ContainsKey(tokens[0]))
					LazyLoadingTable.Add(tokens[0], []);

				string path = Program.ErbDir + tokens[1];
				if (!LazyLoadingFiles.Add(path))
					path = LazyLoadingFiles.First(x => x == path);
				LazyLoadingTable[tokens[0]].Add(path); // 로딩할때 써야 하므로 상위경로를 넣어줘야 함.
			}
		}
		catch (Exception e)
		{
			console.PrintSystemLine(string.Format(trsl.LazyLoadingTableReadError.Text, e.Message));
			LazyCurrentLazyStatus = LazyStatus.Error;
			return;
		}
		LazyCurrentLazyStatus = ChangedFiles.Count != 0 || DeletedFiles.Count != 0 ? LazyStatus.UpdateTable : LazyStatus.Loaded;
	}

	private HashSet<string> GetLazyFiles(IEnumerable<KeyValuePair<string, string>> erbFiles)
	{
		// 지연로딩 대상 폴더 목록을 읽고, 파일이 없거나 읽는 데 실패했다면 리턴.
		var paths = LoadLazyLoadingFolders();
		if (paths == null)
			return new HashSet<string>();

		// 전체 파일 리스트에서 설정된 폴더 내에 있는 파일만 뽑아낸다.
		// erbFiles.key에는 ERB 폴더에서 시작하는 상대경로가 들어있으므로 이 값과 설정된 경로를 비교하면 된다.
		// https://stackoverflow.com/questions/4230313/linq-to-sql-join-and-contains-operators
		var ret = from pair in erbFiles
			from path in paths
			where pair.Key.StartsWith(path) //Substring(0, path.Length) == path 
			select pair.Key;
		HashSet<string> files = new(ret);
		return files;
	}

	public void SaveLazyLoadingList(List<FunctionLabelLine> labels, List<KeyValuePair<string, string>> erbFiles)
	{
		HashSet<string> files = GetLazyFiles(erbFiles);

		if (files.Count == 0)
		{
			LazyCurrentLazyStatus = LazyStatus.NoLazy;
			return;
		}

		// 메소드(#FUNCTION으로 정의되는) 함수와 이벤트 함수가 하나라도 있는 파일을 리스트에서 제외한다.
		foreach (FunctionLabelLine label in labels)
		{
			if (!files.Contains(label.Position.Value.Filename))
			{
				continue;
			}

			if (label.IsMethod)
			{
				if (Program.AnalysisMode)
					console.PrintSystemLine(string.Format(trsl.LazyLoadingFileFunctionExcluded.Text,
						label.Position.Value.Filename, label.LabelName));
				files.Remove(label.Position.Value.Filename);
				continue;
			}

			if (label.IsEvent)
			{
				console.PrintSystemLine(string.Format(trsl.LazyLoadingFileEventExcluded.Text, label.Position.Value.Filename,
					label.LabelName));
				files.Remove(label.Position.Value.Filename);
			}
		}

		// 모든 함수에 대해 리스트에 있는 파일에 속해 있을 경우 리스트에 추가하고 저장한다.

		var metafiles = new HashSet<string>();
		using var writer = new StreamWriter(LazyLoadingDataFilePath, false, Encoding.UTF8, 65536);
		using var metawriter = new StreamWriter(LazyLoadingFilesFilePath, false, Encoding.UTF8, 65536);
		try
		{
			foreach (FunctionLabelLine label in labels)
			{
				if (!files.Contains(label.Position.Value.Filename))
					continue;

				writer.WriteLine(SerializeData(label.LabelName, label.Position.Value.Filename));

				if (!metafiles.Add(label.Position.Value.Filename))
					continue;

				var lastWrite = File.GetLastWriteTime(ErbPath(label.Position.Value.Filename)).ToFileTimeUtc();
				metawriter.WriteLine(SerializeData(label.Position.Value.Filename, lastWrite.ToString()));
			}
		}
		catch (Exception e)
		{
			console.PrintSystemLine(string.Format(trsl.LazyLoadingTableSaveError.Text, e.Message));
			LazyCurrentLazyStatus = LazyStatus.Error;
		}
	}

	public bool SavePartialLazyLoadingList(List<FunctionLabelLine> labels)
	{

		var temp_labels = new List<FunctionLabelLine>();
		var lines = new StringBuilder();
		var labelFiles = new HashSet<string>();
		
		foreach (var file in ChangedFiles.ToList())
		{
			var valid = true;
			foreach (FunctionLabelLine label in labels)
			{
				if(label.Position.Value.Filename != file)
					continue;
				
				if (label.IsMethod)
				{
					if (Program.AnalysisMode)
						console.PrintSystemLine(string.Format(trsl.LazyLoadingFileFunctionExcluded.Text,
							label.Position.Value.Filename, label.LabelName));
					valid = false;
					break;
				}

				if (label.IsEvent)
				{
					if (Program.AnalysisMode)
						console.PrintSystemLine(string.Format(trsl.LazyLoadingFileEventExcluded.Text,
							label.Position.Value.Filename, label.LabelName));
					valid = false;
					break;
				}
				temp_labels.Add(label);
			}

			if (!valid || temp_labels.Count == 0)
			{
				ChangedFiles.Remove(file);
			}
			else
			{
				foreach (var label in temp_labels)
				{
					lines.Append(SerializeData(label.LabelName, label.Position.Value.Filename) + '\n');
					labelFiles.Add(label.Position.Value.Filename);
				}
			}
			temp_labels.Clear();
		}

		if (ChangedFiles.Count == 0 && DeletedFiles.Count == 0)
		{
			LazyCurrentLazyStatus = LazyStatus.Loaded;
			return false;
		}
		
		if(ChangedFiles.Count != 0)
			console.PrintSystemLine(trsl.LazyLoadingFilesModified.Text + ChangedFiles.Count);
		if(DeletedFiles.Count != 0)
			console.PrintSystemLine(trsl.LazyLoadingFilesDeleted.Text + DeletedFiles.Count);
		
		using var writer = new StreamWriter(LazyLoadingDataFilePath, false, Encoding.UTF8, 65536);
		using var metawriter = new StreamWriter(LazyLoadingFilesFilePath, false, Encoding.UTF8, 65536);

		writer.Write(lines.ToString());

		foreach (var name in labelFiles)
		{
			var lastWrite = File.GetLastWriteTime(ErbPath(name)).ToFileTimeUtc();
			metawriter.WriteLine(SerializeData(name, lastWrite.ToString()));
		}

		foreach (var item in LazyLoadingTable)
		{
			writer.WriteLine(SerializeData(item.Key, item.Value[0][Program.ErbDir.Length..]));
		}

		foreach (var item in LazyLoadingFilesTable)
		{
			metawriter.WriteLine(SerializeData(item.Key, item.Value.ToString()));
		}

		LazyCurrentLazyStatus = LazyStatus.Loaded;
		return true;
	}

	static string SerializeData(string a, string b)
	{
		return string.Create(a.Length + b.Length + 1, (a, b), (span, tuple) =>
		{
			tuple.a.AsSpan().CopyTo(span);
			span = span[tuple.a.Length..];
			span[0] = Separator;
			span = span[1..];
			tuple.b.AsSpan().CopyTo(span);
		});
	}

	static string ErbPath(string a)
	{
		return string.Create(a.Length + "ERB/".Length, (a, b: "ERB/"), (span, tuple) =>
		{
			tuple.b.AsSpan().CopyTo(span);
			span = span[tuple.b.Length..];
			tuple.a.AsSpan().CopyTo(span);
		});
	}
}