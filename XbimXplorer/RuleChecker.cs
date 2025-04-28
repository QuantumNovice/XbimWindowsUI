using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Xbim.Ifc;
using Xbim.Ifc2x3.ElectricalDomain;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.UtilityResource;

namespace XbimXplorer
{

	enum RuleCheckResult
	{
		Pass,
		Fail,
		NotApplicable
	}

	internal class RuleChecker
	{
		// RULE : 1
		static bool ValidateWiringPullBoxes(IfcStore model, bool debug = false)
		{
			var entities = model.Instances.OfType<IIfcBuildingElementProxy>().ToList();
			var storeyBoxCount = new Dictionary<string, List<IIfcBuildingElementProxy>>();
			var levelElevations = new Dictionary<string, double>();
			bool isValid = false;

			foreach (var entity in entities)
			{
				if (((string)entity.Name)?.StartsWith("Wiring Pull Box") == true)
				{
					var storey = entity.ContainedInStructure?
						.Select(r => r.RelatingStructure)
						.OfType<IIfcBuildingStorey>()
						.FirstOrDefault();

					if (storey != null)
					{
						string levelName = storey.Name;
						double elevation = (double)storey.Elevation;

						if (debug)
						{
							Console.WriteLine($"{entity.Name} at Level {levelName}");
							Console.WriteLine($"{entity.Name} at Elevation {elevation}");
						}

						if (!storeyBoxCount.ContainsKey(levelName))
						{
							storeyBoxCount[levelName] = new List<IIfcBuildingElementProxy>();
							levelElevations[levelName] = elevation;
						}
						storeyBoxCount[levelName].Add(entity);
					}
				}
			}

			if (storeyBoxCount.Count > 0)
			{
				var lowestLevel = levelElevations.OrderBy(kv => kv.Value).First().Key;
				isValid = true;

				foreach (var kvp in storeyBoxCount)
				{
					string levelName = kvp.Key;
					var boxes = kvp.Value;

					if (boxes.Count > 1 && levelName != lowestLevel &&
						!levelName.IndexOf("Parking", StringComparison.OrdinalIgnoreCase).Equals(-1) &&
						!levelName.IndexOf("주차", StringComparison.OrdinalIgnoreCase).Equals(-1))
					{
						if (debug)
						{
							Console.WriteLine($"Warning: Multiple wiring pull boxes found on level {levelName}");
						}
						isValid = false;
					}
				}
			}

			return isValid;
		}


		static bool ValidateElectricDistributionPoints(IfcStore model, bool debug = false)
		{
			var entities = model.Instances.OfType<IfcElectricDistributionPoint>().ToList();
			var storeyPointCount = new Dictionary<string, Dictionary<string, int>>();
			var levelElevations = new Dictionary<string, double>();
			bool isValid = false;

			foreach (var entity in entities)
			{
				var storey = entity.ContainedInStructure?
					.Select(r => r.RelatingStructure)
					.OfType<IIfcBuildingStorey>()
					.FirstOrDefault();

				if (storey != null)
				{
					string levelName = storey.Name;
					double elevation = (double)storey.Elevation;
					string pointName = entity.Name;

					if (debug)
					{
						Console.WriteLine($"{entity.Name} at Level {levelName}");
						Console.WriteLine($"{entity.Name} at Elevation {elevation}");
					}

					if (!storeyPointCount.ContainsKey(levelName))
					{
						storeyPointCount[levelName] = new Dictionary<string, int>();
						levelElevations[levelName] = elevation;
					}

					if (!storeyPointCount[levelName].ContainsKey(pointName))
					{
						storeyPointCount[levelName][pointName] = 0;
					}
					storeyPointCount[levelName][pointName]++;
				}
			}

			if (storeyPointCount.Count > 0)
			{
				var lowestLevel = levelElevations.OrderBy(kv => kv.Value).First().Key;
				isValid = true;

				foreach (var kvp in storeyPointCount)
				{
					string levelName = kvp.Key;
					var points = kvp.Value;

					foreach (var point in points)
					{
						if (point.Value > 2 && levelName != lowestLevel &&
							!levelName.IndexOf("Parking", StringComparison.OrdinalIgnoreCase).Equals(-1) &&
							!levelName.IndexOf("주차", StringComparison.OrdinalIgnoreCase).Equals(-1))
						{
							if (debug)
							{
								Console.WriteLine($"Warning: More than two electric distribution points with name '{point.Key}' found on level {levelName}");
							}
							isValid = false;
						}
					}
				}
			}

			return isValid;
		}

		static bool HasWiringPullBoxes(IfcStore model)
		{
			return model.Instances.OfType<IIfcBuildingElementProxy>().Any(e => ((string)e.Name)?.StartsWith("Wiring Pull Box") == true);
		}

		static bool HasElectricDistributionPoints(IfcStore model)
		{
			return model.Instances.OfType<IfcElectricDistributionPoint>().Any();
		}

		public static RuleCheckResult valdiate_electric_distribution(IfcStore model)
		{
			

			bool c1 = HasWiringPullBoxes(model);
			bool c2 = HasElectricDistributionPoints(model);

			if (!(c1 && c2))
			{
				return RuleCheckResult.NotApplicable;
			}

			bool validation_flag = false;
			bool validation_flag1 = false;
			if (HasWiringPullBoxes(model))
			{

				bool v1 = ValidateWiringPullBoxes(model);
				validation_flag = v1;
			}
			if (HasElectricDistributionPoints(model))
			{
				bool v2 = ValidateElectricDistributionPoints(model);
				validation_flag1 = v2;
			}

			//Console.WriteLine("Validation Result : " + (validation_flag || validation_flag1));

			if (validation_flag || validation_flag1){
				return RuleCheckResult.Pass;
			}
			return RuleCheckResult.Fail;
		}

		// RULE : 2

		static int GetPortCount(IIfcFlowSegment segment)
		{
			return segment.HasPorts?.Count() ?? 0;
		}

		public static bool is_flow_segment_present(IfcStore model)
		{
			return model.Instances.OfType<IIfcFlowSegment>().Any();
		}	

		public static List<IfcGloballyUniqueId> disconnected_flow_segements(IfcStore model)
		{
			var flowSegments = model.Instances.OfType<IIfcFlowSegment>();
			List<IfcGloballyUniqueId> disconnectedSegments = new List<IfcGloballyUniqueId>();
			foreach (var segment in flowSegments)
			{
				var connectedSegments = GetPortCount(segment);
				if (connectedSegments == 0)
				{
					//Console.WriteLine($"FlowSegment: {segment.Name} - Connected Segments: {connectedSegments}, {segment.GlobalId}");
					disconnectedSegments.Add(segment.GlobalId);
				}
			}
			return disconnectedSegments;
		}

		// RULE : 3
		public static RuleCheckResult check_wifi_ap(IfcStore model)
		{
			var found = model.Instances.OfType<IIfcCommunicationsAppliance>();
			if (found.Count() > 0)
			{
				return RuleCheckResult.Pass;

			}
			else
			{
				// searching for revit by name
				var proxy = model.Instances.OfType<IIfcBuildingElementProxy>();
				foreach (var p in proxy)
				{
					if (((string)p.Name).IndexOf("AP", StringComparison.OrdinalIgnoreCase) >= 0  && ((string)p.Name).IndexOf("WEA", StringComparison.OrdinalIgnoreCase)>=0)
					{
						
						return RuleCheckResult.Pass;
					}
				}
			}

			

			return RuleCheckResult.Fail;
		}

		// RULE :4
		public static RuleCheckResult check_battery(IfcStore model)
		{
			// searching for revit by name
			var proxy = model.Instances.OfType<IIfcBuildingElementProxy>();
			foreach (var p in proxy)
			{
				if (((string)p.Name).IndexOf("battery", StringComparison.OrdinalIgnoreCase) >= 0 )
				{

					return RuleCheckResult.Pass;
				}
			}
			
			return RuleCheckResult.Fail;
		}

		// RULE : 5
		public static RuleCheckResult elevator_check(IfcStore model)
		{
			// searching for revit by name
			var proxy = model.Instances.OfType<IIfcBuildingElementProxy>();
			foreach (var p in proxy)
			{
				if (((string)p.Name).IndexOf("elevator", StringComparison.OrdinalIgnoreCase) >= 0)
				{

					return RuleCheckResult.Pass;
				}
			}

			return RuleCheckResult.Fail;
		}

		// RULE : 
		public static RuleCheckResult check_transformer(IfcStore model)
		{
			// searching for revit by name
			var proxy = model.Instances.OfType<IIfcBuildingElementProxy>();
			foreach (var p in proxy)
			{
				if (((string)p.Name).IndexOf("transformer", StringComparison.OrdinalIgnoreCase) >= 0)
				{

					return RuleCheckResult.Pass;
				}
			}

			return RuleCheckResult.Fail;
		}

		// RULE : 
		public static RuleCheckResult check_weather_proof_receptacle(IfcStore model)
		{
			// searching for revit by name
			var proxy = model.Instances.OfType<IIfcBuildingElementProxy>();
			foreach (var p in proxy)
			{
				if (((string)p.Name).IndexOf("Weather Proof Receptacle", StringComparison.OrdinalIgnoreCase) >= 0)
				{

					return RuleCheckResult.Pass;
				}
			}

			return RuleCheckResult.Fail;
		}

		// RULE : 
		public static RuleCheckResult check_pv_inverter(IfcStore model)
		{
			// searching for revit by name
			var proxy = model.Instances.OfType<IIfcBuildingElementProxy>();
			foreach (var p in proxy)
			{
				if (((string)p.Name).IndexOf("PV Inverter", StringComparison.OrdinalIgnoreCase) >= 0)
				{

					return RuleCheckResult.Pass;
				}
			}

			return RuleCheckResult.Fail;
		}

		// RULE : 
		public static RuleCheckResult check_disconnect_switch(IfcStore model)
		{
			// searching for revit by name
			var proxy = model.Instances.OfType<IIfcBuildingElementProxy>();
			foreach (var p in proxy)
			{
				if (((string)p.Name).IndexOf("Disconnect Switch", StringComparison.OrdinalIgnoreCase) >= 0)
				{

					return RuleCheckResult.Pass;
				}
			}

			return RuleCheckResult.Fail;
		}

	}
}
