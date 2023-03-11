using System.Collections.Generic;
using System.IO;
using System.Reflection;

using HarmonyLib;

using PhantomBrigade;
using PhantomBrigade.Data;
using PhantomBrigade.Overworld.View;

using QFSW.QC;

using UnityEngine;

namespace EchKode.PBMods.HeightSample
{
	using CommandList = List<(string QCName, string Description, MethodInfo Method)>;

	static partial class OverworldInspector
	{
		private const string inputCoordinatesFileName = "xz.txt";
		private const string outputCoordinatesFileName = "xyz.txt";
		private const string delimiter =  ",";

		internal static CommandList Commands() => new CommandList()
		{
			("ow.sample-coords", "Generates 3D coordinates from a list of 2D coordinates", AccessTools.DeclaredMethod(typeof(OverworldInspector), nameof(SampleCoordinates))),
		};

		static void SampleCoordinates()
		{
			if (!IDUtility.IsGameState(GameStates.overworld))
			{
				QuantumConsole.Instance.LogToConsole("Command is available only in the overworld screen");
				return;
			}

			var inputFilePath = Path.Combine(ModLink.modPath, inputCoordinatesFileName);
			if (!File.Exists(inputFilePath))
			{
				QuantumConsole.Instance.LogToConsole("Error: can't find the 2D coordinates file");
				QuantumConsole.Instance.LogToConsole("It should be saved to: {inputFilePath}");
				return;
			}

			var viewParent = OverworldSceneHelper.GetViewParent();
			if (viewParent == null)
			{
				QuantumConsole.Instance.LogToConsole("Internal error -- can't get transform parent from OverworldSceneHelper");
				return;
			}

			if (viewParent.gameObject == null)
			{
				QuantumConsole.Instance.LogToConsole("Internal error --  not a transform without a gameobject");
				return;
			}

			var oview = viewParent.gameObject.GetComponentInChildren<OverworldView>(true);
			if (oview == null)
			{
				QuantumConsole.Instance.LogToConsole("Internal error -- no OverworldView component on gameobject");
				return;
			}

			if (oview.grounder == null)
			{
				QuantumConsole.Instance.LogToConsole("Internal error -- grounder is null on OverworldView");
				return;
			}

			var clone = Object.Instantiate(oview);
			clone.grounder.groundedHolder.DetachChildren();
			foreach (var grounded in clone.grounder.groundedObjects)
			{
				Object.Destroy(grounded);
			}
			clone.grounder.groundedObjects.Clear();

			var mi = AccessTools.DeclaredMethod(typeof(OverworldViewHelperGround), "FillGroundedObjects");
			if (mi == null)
			{
				QuantumConsole.Instance.LogToConsole("Internal error -- can't FillGroundedObjects() method through reflection");
				Object.Destroy(clone);
				return;
			}

			var bounds = DataLinkerSettingsProvinces.data.worldSize;
			var offset = DataLinkerSettingsProvinces.data.worldOffset;
			var coords = new List<Vector3>();
			var i = 0;
			foreach (var line in File.ReadAllLines(inputFilePath))
			{
				i += 1;
				var parts = line.Split(new string[] { delimiter }, System.StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length != 2)
				{
					QuantumConsole.Instance.LogToConsole($"[{i}] wrong number of parts | count: {parts.Length}");
					continue;
				}
				if (!float.TryParse(parts[0], out var x))
				{
					QuantumConsole.Instance.LogToConsole($"[{i}] x parse error | value: {parts[0]}");
					continue;
				}
				var xoff = x + offset.x;
				if (xoff < 0f || xoff > bounds.x)
				{
					QuantumConsole.Instance.LogToConsole($"[{i}] x oob | value: {x}");
					continue;
				}
				if (!float.TryParse(parts[1], out var z))
				{
					QuantumConsole.Instance.LogToConsole($"[{i}] z parse error | value: {parts[1]}");
					continue;
				}
				var zoff = z + offset.z;
				if (zoff < 0f || zoff > bounds.z)
				{
					QuantumConsole.Instance.LogToConsole($"[{i}] z oob | value: {z}");
					continue;
				}
				coords.Add(new Vector3(x, 0f, z));
			}
			
			if (coords.Count == 0)
			{
				QuantumConsole.Instance.LogToConsole($"Error -- didn't find any coordinates in {inputFilePath}");
				QuantumConsole.Instance.LogToConsole($"The coordinates should be stored one per line in the format x{delimiter}z");
				return;
			}

			QuantumConsole.Instance.LogToConsole($"2D coordinates to sample: {coords.Count}");

			foreach (var coord in coords)
			{
				var go = new GameObject();
				go.transform.position = new Vector3(coord.x, 0f, coord.z);
				go.transform.parent = clone.grounder.groundedHolder;
			}

			mi.Invoke(clone.grounder, null);
			clone.grounder.GroundAll();

			var outputFilePath = Path.Combine(ModLink.modPath, outputCoordinatesFileName);
			var sampleCount = 0;
			using (var outp = new StreamWriter(outputFilePath))
			{
				foreach (var grounded in clone.grounder.groundedObjects)
				{
					outp.WriteLine(
						"{1:F4}{0}{2:F4}{0}{3:F4}",
						delimiter,
						grounded.position.x,
						grounded.position.y,
						grounded.position.z);
					sampleCount += 1;
				}
			}

			Object.Destroy(clone);

			QuantumConsole.Instance.LogToConsole($"Samples | in: {coords.Count} | out: {sampleCount}");
			QuantumConsole.Instance.LogToConsole($"Output file: {outputFilePath}");
		}
	}
}
