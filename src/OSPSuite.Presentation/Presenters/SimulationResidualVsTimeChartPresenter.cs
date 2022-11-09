﻿using System.Collections.Generic;
using System.Linq;
using OSPSuite.Assets;
using OSPSuite.Core.Chart;
using OSPSuite.Core.Chart.Simulations;
using OSPSuite.Core.Domain;
using OSPSuite.Core.Domain.Data;
using OSPSuite.Core.Domain.ParameterIdentifications;
using OSPSuite.Core.Domain.Repositories;
using OSPSuite.Core.Domain.Services;
using OSPSuite.Core.Extensions;
using OSPSuite.Core.Services;
using OSPSuite.Presentation.Presenters.Charts;
using OSPSuite.Presentation.Services.Charts;
using OSPSuite.Presentation.Views;
using OSPSuite.Utility.Extensions;

namespace OSPSuite.Presentation.Presenters
{
   public interface ISimulationResidualVsTimeChartPresenter : IChartPresenter<SimulationResidualVsTimeChart>,
      ISimulationAnalysisPresenter
   {
      ISimulationVsObservedDataView View { get; }
   }

   public class SimulationResidualVsTimeChartPresenter : SimulationVsObservedDataChartPresenter<SimulationResidualVsTimeChart>,
      ISimulationResidualVsTimeChartPresenter
   {
      private readonly IObservedDataRepository _observedDataRepository;
      private readonly IResidualsVsTimeChartService _residualsVsTimeChartService;
      private readonly IResidualCalculator _residualCalculator;
      private DataRepository _zeroRepository;
      private IReadOnlyCollection<OutputResiduals> _allOutputResiduals;

      public SimulationResidualVsTimeChartPresenter(ISimulationVsObservedDataView view, ChartPresenterContext chartPresenterContext,
         IObservedDataRepository observedDataRepository, IResidualCalculatorFactory residualCalculatorFactory, IResidualsVsTimeChartService residualsVsTimeChartService)
         : base(view, chartPresenterContext, ApplicationIcons.PredictedVsObservedAnalysis,
            PresenterConstants.PresenterKeys.SimulationPredictedVsObservedChartPresenter)
      {
         _observedDataRepository = observedDataRepository;
         _residualCalculator = residualCalculatorFactory.CreateFor(new ParameterIdentificationConfiguration());
         _residualsVsTimeChartService = residualsVsTimeChartService;
      }

      protected override void UpdateAnalysis()
      {
         var simulationResidual = _residualCalculator.Calculate(_simulation.ResultsDataRepository, _simulation.OutputMappings.All);
         _allOutputResiduals = simulationResidual.AllOutputResiduals;
         if (!getAllAvailableObservedData().Any())
            return;

         _zeroRepository = _residualsVsTimeChartService.AddZeroMarkerCurveToChart(Chart, minObservedDataTime(), maxObservedDataTime());
         AddDataRepositoriesToEditor(new[] { _zeroRepository });

         if (ChartIsBeingCreated) 
            _residualsVsTimeChartService.ConfigureChartAxis(Chart);

         UpdateChartFromTemplate();
         View.SetTotalError(simulationResidual.TotalError);
      }

      protected override void AddRunResultToChart()
      {
         _allOutputResiduals.GroupBy(x => x.FullOutputPath).Each(addOutputToScatter);
      }

      private void addOutputToScatter(IGrouping<string, OutputResiduals> outputMappingsByOutput)
      {
         var fullOutputPath = outputMappingsByOutput.Key;
         var shouldShowInLegend = true;
         foreach (var outputMapping in outputMappingsByOutput)
         {
            var dataRepository = getOrCreateScatterDataRepositoryFor(outputMapping);
            AddResultRepositoryToEditor(dataRepository);
            var visibleInLegend = shouldShowInLegend;

            AddCurvesFor(dataRepository, (column, curve) =>
            {
               SelectColorForPath(fullOutputPath);
               UpdateColorForPath(curve, fullOutputPath);
               curve.Name = fullOutputPath;
               curve.Description = outputMapping.ObservedDataName;
               curve.Symbol = Symbols.Circle;
               curve.LineStyle = LineStyles.None;
               curve.VisibleInLegend = visibleInLegend;
            });

            shouldShowInLegend = false;
         }
      }

      private float minObservedDataTime()
      {
         return getAllAvailableObservedData().Select(x => x.BaseGrid.Values.First()).Min();
      }

      private float maxObservedDataTime()
      {
         return getAllAvailableObservedData().Select(x => x.BaseGrid.Values.Last()).Max();
      }

      private DataRepository getOrCreateScatterDataRepositoryFor(OutputResiduals outputResidual) =>
         _residualsVsTimeChartService.GetOrCreateScatterDataRepositoryInChart(Chart, outputResidual);

      private IEnumerable<DataRepository> getAllAvailableObservedData()
      {
         return _observedDataRepository.AllObservedDataUsedBy(_simulation)
            .Distinct()
            .OrderBy(x => x.Name);
      }

      public override void Clear()
      {
         base.Clear();
         Chart.RemoveCurvesForDataRepository(_zeroRepository);
      }
   }
}