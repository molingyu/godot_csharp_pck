using System;
using Godot;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Godot.Bridge;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using FileAccess = Godot.FileAccess;

public partial class Main : Node2D
{
	[Export] private FileDialog _fileDialog;
	[Export] private Button _loadButton;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_fileDialog.FileSelected += OnFileDialogFileSelected;
		_loadButton.Pressed += LoadPck;
	}

	private void LoadPck()
	{
		_fileDialog.PopupCentered();
	}

	private void OnFileDialogFileSelected(string path)
	{
		var loadResult = ProjectSettings.LoadResourcePack(path);
		if (!loadResult)
		{
			GD.Print("Failed to load DLC pck!");
			return;
		}

		var codes = ExtractCodeFromPck("res://dlc");
		var assembly = CompileCode(codes);
		if (assembly == null)
		{
			GD.Print("Failed to compile code!");
			return;
		}

		var result = assembly.GetType("DlcScene");
		GD.Print(result == null ? "Type 'DlcScene' not found in DlcAssembly!" : "Type 'DlcScene' found in DlcAssembly!");
		ScriptManagerBridge.LookupScriptsInAssembly(assembly);
		LoadDlcScene();
	}

	private List<string> ExtractCodeFromPck(string path)
	{
		var result = new List<string>();
		var paths = DirAccess.GetFilesAt(path);
		foreach (var file in paths)
		{
			if (Path.GetExtension(file) == ".cs")
			{
				var code = ResourceLoader.Load<CSharpScript>($"{path}/{file}");
				result.Add(code.SourceCode);
			}
		}
		return result;
	}

	private Assembly CompileCode(List<string> codes)
	{
		var syntaxTrees = codes.Select(code => CSharpSyntaxTree.ParseText(code)).ToArray();
		var references = AppDomain.CurrentDomain.GetAssemblies()
			.Where(a => !a.IsDynamic)
			.Where(a => a.Location != "")
			.Select(a => MetadataReference.CreateFromFile(a.Location))
			.Cast<MetadataReference>();

		var compilation = CSharpCompilation.Create("DlcAssembly")
			.WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
			.AddReferences(references)
			.AddSyntaxTrees(syntaxTrees);

		using var ms = new MemoryStream();
		var result = compilation.Emit(ms);
		if (!result.Success)
		{
			foreach (var diagnostic in result.Diagnostics)
			{
				GD.Print(diagnostic.ToString());
			}
			return null;
		}

		ms.Seek(0, SeekOrigin.Begin);
		return AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly())?.LoadFromStream(ms);
	}

	private void LoadDlcScene()
	{
		var packedScene = ResourceLoader.Load<PackedScene>("res://dlc/dlc_scene.tscn");
		var sceneNode = packedScene.Instantiate();
		AddChild(sceneNode);
		sceneNode._Ready();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
