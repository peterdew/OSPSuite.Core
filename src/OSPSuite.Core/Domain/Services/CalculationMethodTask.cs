using System.Collections.Generic;
using System.Linq;
using OSPSuite.Assets;
using OSPSuite.Core.Domain.Builder;
using OSPSuite.Core.Domain.Descriptors;
using OSPSuite.Core.Domain.Formulas;
using OSPSuite.Core.Domain.Mappers;
using OSPSuite.Core.Extensions;
using OSPSuite.Utility.Exceptions;
using OSPSuite.Utility.Extensions;

namespace OSPSuite.Core.Domain.Services
{
   internal interface ICalculationMethodTask
   {
      void MergeCalculationMethodInModel(ModelConfiguration modelConfiguration);
   }

   internal class CalculationMethodTask : ICalculationMethodTask
   {
      private readonly IFormulaTask _formulaTask;
      private readonly IKeywordReplacerTask _keywordReplacerTask;
      private readonly IFormulaBuilderToFormulaMapper _formulaMapper;
      private readonly IParameterBuilderToParameterMapper _parameterMapper;
      private readonly IObjectPathFactory _objectPathFactory;
      private EntityDescriptorMapList<IContainer> _allContainers;
      private IList<IParameter> _allBlackBoxParameters;
      private IModel _model;
      private SimulationConfiguration _simulationConfiguration;
      private SimulationBuilder _simulationBuilder;
      private ReplacementContext _replacementContext;

      public CalculationMethodTask(
         IFormulaTask formulaTask,
         IKeywordReplacerTask keywordReplacerTask,
         IFormulaBuilderToFormulaMapper formulaMapper,
         IParameterBuilderToParameterMapper parameterMapper,
         IObjectPathFactory objectPathFactory)
      {
         _formulaTask = formulaTask;
         _keywordReplacerTask = keywordReplacerTask;
         _formulaMapper = formulaMapper;
         _parameterMapper = parameterMapper;
         _objectPathFactory = objectPathFactory;
      }

      public void MergeCalculationMethodInModel(ModelConfiguration modelConfiguration)
      {
         try
         {
            (_model, _simulationBuilder, _replacementContext)  = modelConfiguration;
            _simulationConfiguration = modelConfiguration.SimulationConfiguration;
            _allContainers = _model.Root.GetAllContainersAndSelf<IContainer>().ToEntityDescriptorMapList();
            _allBlackBoxParameters = _model.Root.GetAllChildren<IParameter>().Where(p => p.Formula.IsBlackBox()).ToList();
            foreach (var calculationMethod in _simulationConfiguration.AllCalculationMethods)
            {
               var allMoleculesUsingMethod = allMoleculesUsing(calculationMethod, _simulationBuilder.Molecules).ToList();

               createFormulaForBlackBoxParameters(calculationMethod, allMoleculesUsingMethod);

               addHelpParametersFor(calculationMethod, allMoleculesUsingMethod);
            }
         }
         finally
         {
            _simulationConfiguration = null;
            _simulationBuilder = null;
            _allContainers.Clear();
            _allBlackBoxParameters.Clear();
            _model = null;
         }
      }

      private void addHelpParametersFor(CoreCalculationMethod calculationMethod, IList<MoleculeBuilder> allMoleculesUsingMethod)
      {
         foreach (var helpParameter in calculationMethod.AllHelpParameters())
         {
            var containerDescriptor = calculationMethod.DescriptorFor(helpParameter);
            foreach (var molecule in allMoleculesUsingMethod)
            {
               foreach (var container in allMoleculeContainersFor(containerDescriptor, molecule))
               {
                  var existingParameter = container.Parameter(helpParameter.Name);
                  //does not exist yet
                  if (existingParameter == null)
                  {
                     var parameter = _parameterMapper.MapFrom(helpParameter, _simulationBuilder);
                     container.Add(parameter);
                     replaceKeyWordsIn(parameter, molecule.Name);
                  }
                  else if (!formulasAreTheSameForParameter(existingParameter, helpParameter.Formula, molecule.Name))
                     throw new OSPSuiteException(Error.HelpParameterAlreadyDefinedWithAnotherFormula(calculationMethod.Name, _objectPathFactory.CreateAbsoluteObjectPath(helpParameter).ToString()));
               }
            }
         }
      }

      private void createFormulaForBlackBoxParameters(CoreCalculationMethod calculationMethod, IList<MoleculeBuilder> allMoleculesUsingMethod)
      {
         foreach (var formula in calculationMethod.AllOutputFormulas())
         {
            var parameterDescriptor = calculationMethod.DescriptorFor(formula);
            foreach (var molecule in allMoleculesUsingMethod)
            {
               foreach (var parameter in allMoleculeParameterForFormula(parameterDescriptor, molecule))
               {
                  //not a black box parameter. Should not be overridden by cm
                  if (parameterIsNotBlackBoxParameter(parameter))
                     continue;

                  //parameter formula was not set yet
                  if (parameter.Formula.IsBlackBox())
                  {
                     parameter.Formula = _formulaMapper.MapFrom(formula, _simulationBuilder);
                     replaceKeyWordsIn(parameter, molecule.Name);
                  }
                  else
                  {
                     if (!formulasAreTheSameForParameter(parameter, formula, molecule.Name))
                        throw new OSPSuiteException(Error.TwoDifferentFormulaForSameParameter(parameter.Name, _objectPathFactory.CreateAbsoluteObjectPath(parameter).ToPathString()));
                  }
               }
            }
         }
      }

      private void replaceKeyWordsIn(IParameter parameter, string moleculeName)
      {
         _keywordReplacerTask.ReplaceIn(parameter, moleculeName, _replacementContext);
         //check if parameter is in neighborhood. In that case, retrieve the neighborhood and replace the keywords as well
         var neighborhood = neighborhoodAncestorFor(parameter);
         if (neighborhood == null) return;
         _keywordReplacerTask.ReplaceIn(neighborhood, _replacementContext);
      }

      private static Neighborhood neighborhoodAncestorFor(IEntity entity)
      {
         if (entity == null)
            return null;

         if (entity.IsAnImplementationOf<Neighborhood>())
            return entity.DowncastTo<Neighborhood>();

         return neighborhoodAncestorFor(entity.ParentContainer);
      }

      private bool formulasAreTheSameForParameter(IParameter originalParameter, IFormula calculationMethodFormula, string moleculeName)
      {
         var previousFormula = originalParameter.Formula;
         //needs to use the parameter in order to keep the hierarchy. Hence we set the formula in the parameter
         originalParameter.Formula = _formulaMapper.MapFrom(calculationMethodFormula, _simulationBuilder);

         //check  if the formula set are the same. it is necessary to replace keywords before doing that
         replaceKeyWordsIn(originalParameter, moleculeName);

         try
         {
            return _formulaTask.FormulasAreTheSame(originalParameter.Formula, previousFormula);
         }
         finally
         {
            //reset the origianl formula in any case
            originalParameter.Formula = previousFormula;
         }
      }

      private bool parameterIsNotBlackBoxParameter(IParameter parameter)
      {
         return !_allBlackBoxParameters.Contains(parameter);
      }

      private IEnumerable<MoleculeBuilder> allMoleculesUsing(CoreCalculationMethod calculationMethod, IReadOnlyCollection<MoleculeBuilder> molecules)
      {
         return molecules
            .Where(molecule => molecule.IsFloatingXenobiotic)
            .SelectMany(molecule => molecule.UsedCalculationMethods, (molecule, usedCalculationMethod) => new {molecule, usedCalculationMethod})
            .Where(x => x.usedCalculationMethod.CalculationMethod == calculationMethod.Name)
            .Select(x => x.molecule);
      }

      private IEnumerable<IContainer> allMoleculeContainersFor(DescriptorCriteria containerDescriptor, MoleculeBuilder molecule)
      {
         return from container in _allContainers.AllSatisfiedBy(containerDescriptor)
            let moleculeContainer = container.GetSingleChildByName<IContainer>(molecule.Name)
            where moleculeContainer != null
            select moleculeContainer;
      }

      private IEnumerable<IParameter> allMoleculeParameterForFormula(ParameterDescriptor parameterDescriptor, MoleculeBuilder molecule)
      {
         return from container in allMoleculeContainersFor(parameterDescriptor.ContainerCriteria, molecule)
            let parameter = container.GetSingleChildByName<IParameter>(parameterDescriptor.ParameterName)
            where parameter != null
            select parameter;
      }
   }
}