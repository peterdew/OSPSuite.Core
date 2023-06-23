using System.Linq;
using OSPSuite.Core.Domain.Builder;
using OSPSuite.Core.Domain.Mappers;
using OSPSuite.Core.Extensions;
using OSPSuite.Utility.Extensions;
using OSPSuite.Utility.Visitor;

namespace OSPSuite.Core.Domain.Services
{
   internal interface IReactionCreator
   {
      bool CreateReaction(ReactionBuilder reactionBuilder, ModelConfiguration modelConfiguration);
   }

   internal class ReactionCreator : IReactionCreator, IVisitor<IContainer>
   {
      private readonly IReactionBuilderToReactionMapper _reactionMapper;
      private readonly IKeywordReplacerTask _keywordReplacerTask;
      private readonly IContainerTask _containerTask;
      private readonly IParameterBuilderCollectionToParameterCollectionMapper _parameterMapper;
      private IModel _model;
      private bool _createdInstance;
      private SimulationBuilder _simulationBuilder;
      private ReactionBuilder _reactionBuilder;

      public ReactionCreator(IReactionBuilderToReactionMapper reactionMapper, IKeywordReplacerTask keywordReplacerTask,
         IContainerTask containerTask,
         IParameterBuilderCollectionToParameterCollectionMapper parameterMapper)
      {
         _reactionMapper = reactionMapper;
         _keywordReplacerTask = keywordReplacerTask;
         _containerTask = containerTask;
         _parameterMapper = parameterMapper;
      }

      public bool CreateReaction(ReactionBuilder reactionBuilder, ModelConfiguration modelConfiguration)
      {
         _reactionBuilder = reactionBuilder;
         (_model, _simulationBuilder) = modelConfiguration;
         try
         {
            //global container should be created before creating local reaction so that path replacement works
            var reactionGlobalContainer = createReactionPropertiesContainer();
            _createdInstance = false;
            _model.Root.AcceptVisitor(this);

            //reaction was not created. No need to create a global container
            if (!_createdInstance)
               _model.Root.RemoveChild(reactionGlobalContainer);

            return _createdInstance;
         }
         finally
         {
            _reactionBuilder = null;
            _model = null;
            _simulationBuilder = null;
         }
      }

      private IContainer createReactionPropertiesContainer()
      {
         var globalReactionContainer = _containerTask.CreateOrRetrieveSubContainerByName(_model.Root, _reactionBuilder.Name)
            .WithContainerType(ContainerType.Reaction)
            .WithIcon(_reactionBuilder.Icon)
            .WithDescription(_reactionBuilder.Description);

         _simulationBuilder.AddBuilderReference(globalReactionContainer, _reactionBuilder);

         //"Local"-Parameters will be filled in elsewhere (by the Reaction-Mapper)
         _parameterMapper.MapGlobalOrPropertyFrom(_reactionBuilder, _simulationBuilder).Each(globalReactionContainer.Add);

         _keywordReplacerTask.ReplaceIn(globalReactionContainer, _model.Root, _reactionBuilder.Name);
         return globalReactionContainer;
      }

      public void Visit(IContainer container)
      {
         if (!canCreateReactionIn(container)) return;
         _createdInstance = true;
         container.Add(_reactionMapper.MapFromLocal(_reactionBuilder, container, _simulationBuilder));
      }

      private bool canCreateReactionIn(IContainer container)
      {
         var allMoleculeNames = container.GetChildren<MoleculeAmount>().Select(x => x.Name).ToList();

         return container.Mode == ContainerMode.Physical
                && allMoleculeNames.ContainsAll(_reactionBuilder.Educts.Select(x => x.MoleculeName))
                && allMoleculeNames.ContainsAll(_reactionBuilder.Products.Select(x => x.MoleculeName))
                && allMoleculeNames.ContainsAll(_reactionBuilder.ModifierNames)
                && containerMatchesReactionCriteria(container);
      }

      private bool containerMatchesReactionCriteria(IContainer container)
      {
         //No criteria defined in the reaction => the container is a match
         return !_reactionBuilder.ContainerCriteria.Any() || _reactionBuilder.ContainerCriteria.IsSatisfiedBy(container);
      }
   }
}