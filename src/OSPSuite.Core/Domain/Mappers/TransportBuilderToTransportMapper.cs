using System.Collections.Generic;
using OSPSuite.Core.Domain.Builder;
using OSPSuite.Utility.Extensions;

namespace OSPSuite.Core.Domain.Mappers
{
   /// <summary>
   ///    Mapper for the creation of a Transport in the model from <see cref="ITransportBuilder" />.
   /// </summary>
   public interface ITransportBuilderToTransportMapper : IBuilderMapper<ITransportBuilder, ITransport>
   {
   }

   /// <summary>
   ///    Mapper for the creation of a Transport in the model from <see cref="ITransportBuilder" />.
   /// </summary>
   public class TransportBuilderToTransportMapper : ITransportBuilderToTransportMapper
   {
      private readonly IObjectBaseFactory _objectBaseFactory;
      private readonly IFormulaBuilderToFormulaMapper _formulaMapper;
      private readonly IParameterBuilderCollectionToParameterCollectionMapper _parameterMapper;
      private readonly IProcessRateParameterCreator _processRateParameterCreator;

      public TransportBuilderToTransportMapper(IObjectBaseFactory objectBaseFactory,
         IFormulaBuilderToFormulaMapper formulaMapper,
         IParameterBuilderCollectionToParameterCollectionMapper parameterMapper,
         IProcessRateParameterCreator processRateParameterCreator)
      {
         _objectBaseFactory = objectBaseFactory;
         _formulaMapper = formulaMapper;
         _parameterMapper = parameterMapper;
         _processRateParameterCreator = processRateParameterCreator;
      }

      public ITransport MapFrom(ITransportBuilder transportBuilder, IBuildConfiguration buildConfiguration)
      {
         var transport = _objectBaseFactory.Create<ITransport>()
            .WithName(transportBuilder.Name)
            .WithIcon(transportBuilder.Icon)
            .WithDimension(transportBuilder.Dimension)
            .WithFormula(_formulaMapper.MapFrom(transportBuilder.Formula, buildConfiguration));

         buildConfiguration.AddBuilderReference(transport, transportBuilder);

         addLocalParameters(transport, transportBuilder, buildConfiguration);

         //lastly, add parameter rate transporter if required
         if (transportBuilder.CreateProcessRateParameter)
            transport.Add(processRateParameterFor(transportBuilder, buildConfiguration));

         return transport;
      }

      private void addLocalParameters(ITransport transport, ITransportBuilder transportBuilder, IBuildConfiguration buildConfiguration)
      {
         transport.AddChildren(_parameterMapper.MapLocalFrom(transportBuilder, buildConfiguration));
      }

      private IParameter processRateParameterFor(ITransportBuilder transportBuilder, IBuildConfiguration buildConfiguration)
      {
         var parameter = _processRateParameterCreator.CreateProcessRateParameterFor(transportBuilder, buildConfiguration);

         parameter.AddTag(ObjectPathKeywords.MOLECULE);
         parameter.AddTag(ObjectPathKeywords.NEIGHBORHOOD);
         transportTypeTagsFor(transportBuilder).Each(parameter.AddTag);
         return parameter;
      }

      private IReadOnlyList<string> transportTypeTagsFor(ITransportBuilder transportBuilder)
      {
         if (!transportBuilder.TransportType.Is(TransportType.Active))
            return new[] {Constants.PASSIVE};

         var activeType = transportBuilder.TransportType.Is(TransportType.Influx) ? Constants.INFLUX : Constants.NOT_INFLUX;
         return new[] {Constants.ACTIVE, activeType};
      }
   }
}