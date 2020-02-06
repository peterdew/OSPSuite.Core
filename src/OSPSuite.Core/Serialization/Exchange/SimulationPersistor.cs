﻿using System;
using System.Xml.Linq;
using OSPSuite.Assets;
using OSPSuite.Core.Converter;
using OSPSuite.Core.Domain;
using OSPSuite.Core.Domain.Services;
using OSPSuite.Core.Domain.UnitSystem;
using OSPSuite.Core.Serialization.Xml;
using OSPSuite.Utility.Exceptions;
using IContainer = OSPSuite.Utility.Container.IContainer;

namespace OSPSuite.Core.Serialization.Exchange
{
   public interface ISimulationPersistor
   {
      void Save(SimulationTransfer simulationTransfer, string fileName);
      SimulationTransfer Load(string pkmlFileFullPath, IDimensionFactory dimensionFactory, IObjectBaseFactory objectBaseFactory, IWithIdRepository withIdRepository, ICloneManagerForModel cloneManagerForModel);
   }

   public class SimulationPersistor : ISimulationPersistor
   {
      private readonly IOSPSuiteXmlSerializerRepository _modelingXmlSerializerRepository;
      private readonly IObjectConverterFinder _objectConverterFinder;
      private readonly IReferencesResolver _refResolver;
      private readonly IContainer _container;

      public SimulationPersistor(
         IOSPSuiteXmlSerializerRepository modelingXmlSerializerRepository, 
         IObjectConverterFinder objectConverterFinder, 
         IReferencesResolver refResolver, 
         IContainer container)
      {
         _modelingXmlSerializerRepository = modelingXmlSerializerRepository;
         _objectConverterFinder = objectConverterFinder;
         _refResolver = refResolver;
         _container = container;
      }

      public void Save(SimulationTransfer simulationTransfer, string fileName)
      {
         using (var serializationContext = SerializationTransaction.Create(_container))
         {
            var serializer = _modelingXmlSerializerRepository.SerializerFor(simulationTransfer);
            var element = serializer.Serialize(simulationTransfer, serializationContext);
            element.Save(fileName);
         }
      }

      private void convertXml(XElement sourceElement, int version)
      {
         if (sourceElement == null) return;
         //set version to avoid double conversion in the case of multiple load
         convert(sourceElement, version, x => x.ConvertXml);
         sourceElement.SetAttributeValue(Constants.Serialization.Attribute.VERSION, Constants.PKML_VERSION);
      }

      private void convert<T>(T objectToConvert, int objectVersion, Func<IObjectConverter, Func<T, (int, bool)>> converterAction)
      {
         int version = objectVersion;
         if (version <= PKMLVersion.NON_CONVERTABLE_VERSION)
            throw new OSPSuiteException(Constants.TOO_OLD_PKML);

         while (version != Constants.PKML_VERSION)
         {
            var converter = _objectConverterFinder.FindConverterFor(version);
            var (convertedVersion, _) = converterAction(converter).Invoke(objectToConvert);
            version = convertedVersion;
         }
      }

      public SimulationTransfer Load(string pkmlFileFullPath, IDimensionFactory dimensionFactory, IObjectBaseFactory objectBaseFactory, IWithIdRepository withIdRepository, ICloneManagerForModel cloneManagerForModel)
      {
         SimulationTransfer simulationTransfer;
         int version;
         using (var serializationContext = SerializationTransaction.Create(_container, dimensionFactory, objectBaseFactory, withIdRepository, cloneManagerForModel))
         {
            var element = XElement.Load(pkmlFileFullPath);
            version = element.GetPKMLVersion();

            convertXml(element, version);

            var serializer = _modelingXmlSerializerRepository.SerializerFor<SimulationTransfer>();
            if (!string.Equals(serializer.ElementName, element.Name.LocalName))
               throw new OSPSuiteException(Error.CouldNotLoadSimulationFromFile(pkmlFileFullPath));

            simulationTransfer = serializer.Deserialize<SimulationTransfer>(element, serializationContext);
         }

         _refResolver.ResolveReferencesIn(simulationTransfer.Simulation.Model);
         convert(simulationTransfer, version, x => x.Convert);

         return simulationTransfer;
      }
   }
}