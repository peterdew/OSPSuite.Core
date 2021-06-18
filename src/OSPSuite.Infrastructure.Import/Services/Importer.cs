﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OSPSuite.Assets;
using OSPSuite.Core.Domain;
using OSPSuite.Core.Domain.Data;
using OSPSuite.Core.Domain.Services;
using OSPSuite.Core.Import;
using OSPSuite.Infrastructure.Import.Core;
using OSPSuite.Infrastructure.Import.Core.Mappers;
using OSPSuite.Utility.Collections;
using OSPSuite.Utility.Extensions;
using ImporterConfiguration = OSPSuite.Core.Import.ImporterConfiguration;
using IoC = OSPSuite.Utility.Container.IContainer;

namespace OSPSuite.Infrastructure.Import.Services
{
   public interface IImporter
   {
      IDataSourceFile LoadFile(IReadOnlyList<ColumnInfo> columnInfos, string fileName, IReadOnlyList<MetaDataCategory> metaDataCategories);
      void AddFromFile(IDataFormat format, Cache<string, DataSheet> dataSheets, IReadOnlyList<ColumnInfo> columnInfos, IDataSource alreadyExisting);
      IEnumerable<IDataFormat> AvailableFormats(IUnformattedData data, IReadOnlyList<ColumnInfo> columnInfos, IReadOnlyList<MetaDataCategory> metaDataCategories);
      IEnumerable<string> NamesFromConvention
      (
         string namingConvention,
         string fileName,
         Cache<string, IDataSet> dataSets,
         IEnumerable<MetaDataMappingConverter> mappings
      );
      int GetImageIndex(DataFormatParameter parameter);
      MappingProblem CheckWhetherAllDataColumnsAreMapped(IReadOnlyList<ColumnInfo> dataColumns, IEnumerable<DataFormatParameter> mappings);

      IReadOnlyList<DataRepository> DataSourceToDataSets(IDataSource dataSource, IReadOnlyList<MetaDataCategory> metaDataCategories,
         DataImporterSettings dataImporterSettings, string id);

      (IReadOnlyList<DataRepository> DataRepositories, List<string> MissingSheets) ImportFromConfiguration
      (
         ImporterConfiguration configuration,
         IReadOnlyList<ColumnInfo> columnInfos,
         string fileName,
         IReadOnlyList<MetaDataCategory> metaDataCategories,
         DataImporterSettings dataImporterSettings
      );
   }

   public class Importer : IImporter
   {
      private readonly IoC _container;
      private readonly IDataSourceFileParser _parser;
      private readonly IDataSetToDataRepositoryMapper _dataRepositoryMapper;

      public Importer( IoC container, IDataSourceFileParser parser, IDataSetToDataRepositoryMapper dataRepositoryMapper)
      {
         _container = container;
         _parser = parser;
         _dataRepositoryMapper = dataRepositoryMapper;
      }

      public IEnumerable<IDataFormat> AvailableFormats(IUnformattedData data, IReadOnlyList<ColumnInfo> columnInfos, IReadOnlyList<MetaDataCategory> metaDataCategories)
      {
         return _container.ResolveAll<IDataFormat>()
            .Select(x => (x, x.SetParameters(data, columnInfos, metaDataCategories)))
            .Where(p => p.Item2 > 0)
            .OrderByDescending(p => p.Item2)
            .Select(p => p.x);
      }

      public void AddFromFile(IDataFormat format, Cache<string, DataSheet> dataSheets, IReadOnlyList<ColumnInfo> columnInfos, IDataSource alreadyExisting)
      {
         var dataSets = new Cache<string, IDataSet>();

         foreach (var sheetKeyValue in dataSheets.KeyValues)
         {
            var data = new DataSet();
            data.AddData(format.Parse(sheetKeyValue.Value.RawData, columnInfos));
            dataSets.Add(sheetKeyValue.Key, data);
         }

         foreach (var key in dataSets.Keys)
         {
            IDataSet current;
            if (alreadyExisting.DataSets.Contains(key))
               current = alreadyExisting.DataSets[key];
            else
            {
               current = new DataSet();
               alreadyExisting.DataSets.Add(key, current);
            }
            current.AddData(dataSets[key].Data);
         }
      }

      public IDataSourceFile LoadFile(IReadOnlyList<ColumnInfo> columnInfos, string fileName, IReadOnlyList<MetaDataCategory> metaDataCategories)
      {
         var dataSource = _parser.For(fileName);

         if (dataSource.DataSheets == null) return null;

         dataSource.AvailableFormats = AvailableFormats(dataSource.DataSheets.ElementAt(0).RawData, columnInfos, metaDataCategories).ToList();
         
         if (dataSource.AvailableFormats.Count == 0)
            throw new UnsupportedFormatException(fileName);

         dataSource.Format = dataSource.AvailableFormats.FirstOrDefault();
         
         return dataSource;
      }

      public IEnumerable<string> NamesFromConvention
      (
         string namingConvention,
         string fileName,
         Cache<string, IDataSet> dataSets,
         IEnumerable<MetaDataMappingConverter> mappings
      )
      {
         fileName = Path.GetFileNameWithoutExtension(fileName);
         var counters = new Dictionary<string, int>();
         // Iterate over the list of datasets to generate a unique name for each one based on the naming convention
         return dataSets.KeyValues.SelectMany(ds => ds.Value.Data.Select(s =>
         {
            // Obtain the key first before checking if it already is taken by any other dataset
            var key = s.NameFromConvention(mappings, namingConvention, fileName, ds.Key);
            int counter;
            if (counters.ContainsKey(key))
            {
               counter = counters[key];
            }
            else
            {
               counters.Add(key, 0);
               counter = 0;
            }
            counters[key]++;
            // Only add a number (for making it unique) to the name if the key already existed in the counters
            return key + (counter > 0 ? $"_{counter}" : "");
         })).ToList();
      }

      public int GetImageIndex(DataFormatParameter parameter)
      {
         switch (parameter)
         {
            case MetaDataFormatParameter mp:
               return ApplicationIcons.IconIndex(ApplicationIcons.MetaData);
            case MappingDataFormatParameter mp:
               return ApplicationIcons.IconIndex(ApplicationIcons.UnitInformation);
            case GroupByDataFormatParameter gp:
               return ApplicationIcons.IconIndex(ApplicationIcons.GroupBy);
            default:
               throw new Exception($"{parameter.GetType()} is not currently been handled");
         }
      }

      public MappingProblem CheckWhetherAllDataColumnsAreMapped(IReadOnlyList<ColumnInfo> dataColumns, IEnumerable<DataFormatParameter> mappings)
      {
         var subset = mappings.OfType<MappingDataFormatParameter>().ToList();

         return new MappingProblem()
         {
            MissingMapping = dataColumns
               .Where(col => col.IsMandatory && subset.All(cm =>
                  cm.MappedColumn.Name != col.Name)).Select(col => col.Name)
               .ToList(),
            MissingUnit = subset.Where(cm => cm.MappedColumn.Unit.SelectedUnit == UnitDescription.InvalidUnit && (cm.MappedColumn.ErrorStdDev == null || cm.MappedColumn.ErrorStdDev.Equals(Constants.STD_DEV_ARITHMETIC))).Select(cm => cm.MappedColumn.Name)
               .ToList()
         };
      }

      public IReadOnlyList<DataRepository> DataSourceToDataSets(IDataSource dataSource, IReadOnlyList<MetaDataCategory> metaDataCategories,
         DataImporterSettings dataImporterSettings, string id)
      {
         var dataRepositories = new List<DataRepository>();

         for (var i = 0; i < dataSource.DataSets.SelectMany(ds => ds.Data).Count(); i++)
         {
            var dataRepo = _dataRepositoryMapper.ConvertImportDataSet(dataSource.DataSetAt(i));
            dataRepo.ConfigurationId = id;

            var moleculeDescription = extractMoleculeDescription(metaDataCategories, dataImporterSettings, dataRepo);
            var molecularWeightDescription = dataRepo.ExtendedPropertyValueFor(dataImporterSettings.NameOfMetaDataHoldingMolecularWeightInformation);

            if (moleculeDescription != null)
            {
               if (string.IsNullOrEmpty(molecularWeightDescription))
               {
                  molecularWeightDescription = moleculeDescription;
               }
               else
               {
                  double.TryParse(moleculeDescription, out var moleculeMolWeight);
                  double.TryParse(molecularWeightDescription, out var molWeight);

                  if (!ValueComparer.AreValuesEqual(moleculeMolWeight, molWeight))
                  {
                     throw new InconsistentMoleculeAndMolWeightException();
                  }
               }
            }

            if (!string.IsNullOrEmpty(molecularWeightDescription))
            {
               if (double.TryParse(molecularWeightDescription, out var molWeight))
               {
                  dataRepo.AllButBaseGrid().Each(x => x.DataInfo.MolWeight = molWeight);
               }
            }

            //We remove the extended property of MolWeight to avoid the duplication, since the MolWeight exists also in the DataRepository properties
            dataRepo.ExtendedProperties.Remove(dataImporterSettings.NameOfMetaDataHoldingMolecularWeightInformation);
            dataRepositories.Add(dataRepo);
         }

         return dataRepositories;
      }

      private static string extractMoleculeDescription(IReadOnlyList<MetaDataCategory> metaDataCategories, DataImporterSettings dataImporterSettings, DataRepository dataRepo)
      {
         var metaDataCategoryForMoleculeDescription =
            (metaDataCategories?.FirstOrDefault(md => md.Name == dataImporterSettings.NameOfMetaDataHoldingMoleculeInformation));
         var moleculeDescription = metaDataCategoryForMoleculeDescription?.ListOfValues.FirstOrDefault(v =>
            v.Key == dataRepo.ExtendedPropertyValueFor(dataImporterSettings.NameOfMetaDataHoldingMoleculeInformation)).Value;
         
         return moleculeDescription;
      }

      public (IReadOnlyList<DataRepository> DataRepositories, List<string> MissingSheets) ImportFromConfiguration
      (
         ImporterConfiguration configuration,
         IReadOnlyList<ColumnInfo> columnInfos,
         string fileName,
         IReadOnlyList<MetaDataCategory> metaDataCategories,
         DataImporterSettings dataImporterSettings
      )
      {
         var dataSource = new DataSource(this);
         IDataSourceFile dataSourceFile = null;

         dataSourceFile = LoadFile(columnInfos, fileName, metaDataCategories);

         if (dataSourceFile == null)
         {
            return (Enumerable.Empty<DataRepository>().ToList(), Enumerable.Empty<string>().ToList());
         }

         dataSourceFile.Format.CopyParametersFromConfiguration(configuration);
         var mappings = dataSourceFile.Format.Parameters.OfType<MetaDataFormatParameter>().Select(md => new MetaDataMappingConverter()
         {
            Id = md.MetaDataId,
            Index = sheetName => md.IsColumn ? dataSourceFile.DataSheets[sheetName].RawData.GetColumnDescription(md.ColumnName).Index : -1
         }).Union
         (
            dataSourceFile.Format.Parameters.OfType<GroupByDataFormatParameter>().Select(md => new MetaDataMappingConverter()
            {
               Id = md.ColumnName,
               Index = sheetName => dataSourceFile.DataSheets[sheetName].RawData.GetColumnDescription(md.ColumnName).Index
            })
         );
         dataSource.SetMappings(dataSourceFile.Path, mappings);
         dataSource.NanSettings = configuration.NanSettings;
         dataSource.SetDataFormat(dataSourceFile.Format);
         dataSource.SetNamingConvention(configuration.NamingConventions);
         var sheets = new Cache<string, DataSheet>();
         var missingSheets = new List<string>();
         foreach (var key in configuration.LoadedSheets)
         {
            if (!dataSourceFile.DataSheets.Contains(key))
            {
               missingSheets.Add(key);
               continue;
            }

            sheets.Add(key, dataSourceFile.DataSheets[key]);
         }

         dataSource.AddSheets(sheets, columnInfos, configuration.FilterString);
         return (DataSourceToDataSets(dataSource, metaDataCategories, dataImporterSettings, configuration.Id), missingSheets);
      }
   }

   public class MappingProblem
   {
      public IReadOnlyList<string> MissingMapping { get; set; }
      public IReadOnlyList<string> MissingUnit { get; set; }
   }
}