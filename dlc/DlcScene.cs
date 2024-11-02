using Godot;
using System;

[assembly:AssemblyHasScripts([typeof(DlcScene)])]
[ScriptPath("res://dlc/DlcScene.cs")]
public partial class DlcScene : Node2D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Print("Load DLC pck successfully!");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
