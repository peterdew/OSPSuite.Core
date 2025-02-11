using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OSPSuite.Assets;
using OSPSuite.Core.Domain.Builder;
using OSPSuite.Core.Domain.Services;
using OSPSuite.Utility.Exceptions;
using OSPSuite.Utility.Extensions;
using OSPSuite.Utility.Visitor;

namespace OSPSuite.Core.Domain
{
   public class Module : ObjectBase, IEnumerable<IBuildingBlock>
   {
      private readonly List<IBuildingBlock> _buildingBlocks = new List<IBuildingBlock>();

      private T buildingBlockByType<T>() where T : IBuildingBlock => _buildingBlocks.OfType<T>().SingleOrDefault();
      private IReadOnlyList<T> buildingBlocksByType<T>() where T : IBuildingBlock => _buildingBlocks.OfType<T>().ToList();

      /// <summary>
      /// Module is a PKSim module if created in PKSim and the module version
      /// matches the version when it was first imported
      /// </summary>
      public bool IsPKSimModule => !string.IsNullOrEmpty(PKSimVersion) && Equals(Version, ModuleImportVersion);

      public string ModuleImportVersion { get; set; }

      public string PKSimVersion { get; set; }

      public EventGroupBuildingBlock EventGroups => buildingBlockByType<EventGroupBuildingBlock>();
      public MoleculeBuildingBlock Molecules => buildingBlockByType<MoleculeBuildingBlock>();
      public ObserverBuildingBlock Observers => buildingBlockByType<ObserverBuildingBlock>();
      public ReactionBuildingBlock Reactions => buildingBlockByType<ReactionBuildingBlock>();
      public PassiveTransportBuildingBlock PassiveTransports => buildingBlockByType<PassiveTransportBuildingBlock>();
      public SpatialStructure SpatialStructure => buildingBlockByType<SpatialStructure>();

      public IReadOnlyList<InitialConditionsBuildingBlock> InitialConditionsCollection => buildingBlocksByType<InitialConditionsBuildingBlock>();

      public IReadOnlyList<ParameterValuesBuildingBlock> ParameterValuesCollection => buildingBlocksByType<ParameterValuesBuildingBlock>();

      public string Version => versionCalculation(BuildingBlocks);

      public string VersionWith(ParameterValuesBuildingBlock selectedParameterValues, InitialConditionsBuildingBlock selectedInitialConditions)
      {
         var buildingBlocks = BuildingBlocks.Where(isSingle).ToList();
         
         if(selectedParameterValues != null)
            buildingBlocks.Add(selectedParameterValues);

         if (selectedInitialConditions != null)
            buildingBlocks.Add(selectedInitialConditions);

         return versionCalculation(buildingBlocks);
      }

      private string versionCalculation(IEnumerable<IBuildingBlock> buildingBlocks)
      {
         // Use OrderBy to ensure alphabetical ordering of the typed versions
         return string.Join(string.Empty, buildingBlocks.Select(typedVersionFor).OrderBy(x => x).ToArray()).GetHashCode().ToString();
      }

      private static string typedVersionFor(IBuildingBlock x)
      {
         return $"{x.GetType()}{x.Version}";
      }

      public override string Icon {
         get => IsPKSimModule ? IconNames.PKSimModule : IconNames.Module;
         set
         {
            // Do not set from outside
         }
      }

      public override void UpdatePropertiesFrom(IUpdatable source, ICloneManager cloneManager)
      {
         base.UpdatePropertiesFrom(source, cloneManager);

         if (!(source is Module sourceModule))
            return;

         sourceModule.BuildingBlocks.Select(cloneManager.Clone).Each(Add);
         PKSimVersion = sourceModule.PKSimVersion;
         ModuleImportVersion = sourceModule.ModuleImportVersion;
      }

      public void Add(IBuildingBlock buildingBlock)
      {
         if (isSingle(buildingBlock))
         {
            var type = buildingBlock.GetType();
            var existingBuildingBlock = _buildingBlocks.FirstOrDefault(x => x.IsAnImplementationOf(type));
            if (existingBuildingBlock != null)
               throw new OSPSuiteException(Error.BuildingBlockTypeAlreadyAddedToModule(buildingBlock.Name, type.Name));
         }

         buildingBlock.Module = this;
         _buildingBlocks.Add(buildingBlock);
      }

      private bool isSingle(IBuildingBlock buildingBlock) => !(buildingBlock.IsAnImplementationOf<InitialConditionsBuildingBlock>() || buildingBlock.IsAnImplementationOf<ParameterValuesBuildingBlock>());

      public void Remove(IBuildingBlock buildingBlock)
      {
         buildingBlock.Module = null;
         _buildingBlocks.Remove(buildingBlock);
      }

      public IReadOnlyList<IBuildingBlock> BuildingBlocks => _buildingBlocks;

      public override void AcceptVisitor(IVisitor visitor)
      {
         base.AcceptVisitor(visitor);
         BuildingBlocks.Each(x => x.AcceptVisitor(visitor));
      }

      public IEnumerator<IBuildingBlock> GetEnumerator()
      {
         return _buildingBlocks.GetEnumerator();
      }

      IEnumerator IEnumerable.GetEnumerator()
      {
         return GetEnumerator();
      }
   }
}