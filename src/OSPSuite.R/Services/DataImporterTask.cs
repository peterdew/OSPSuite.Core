﻿using System.Collections.Generic;
using System.Linq;
using OSPSuite.Core.Domain;
using OSPSuite.Core.Domain.Data;
using OSPSuite.Core.Domain.UnitSystem;
using OSPSuite.Core.Import;
using OSPSuite.Core.Serialization.Xml;
using OSPSuite.Infrastructure.Import.Core;
using OSPSuite.Infrastructure.Import.Extensions;
using OSPSuite.Infrastructure.Import.Services;
using OSPSuite.Utility.Extensions;
using ImporterConfiguration = OSPSuite.Core.Import.ImporterConfiguration;

namespace OSPSuite.R.Services
{
   public interface IDataImporterTask
   {
      ImporterConfiguration CreateConfiguration();
      ImporterConfiguration GetConfiguration(string filePath);
      ImporterConfiguration CreateConfigurationFor(string dataPath);
      void SaveConfiguration(ImporterConfiguration configuration, string path);
      IReadOnlyList<DataRepository> ImportExcelFromConfiguration(string configurationPath, string dataPath);
      IReadOnlyList<DataRepository> ImportExcelFromConfiguration(ImporterConfiguration configuration, string dataPath);
      IReadOnlyList<DataRepository> ImportCsvFromConfiguration(string configurationPath, string dataPath, char columnSeparator);
      IReadOnlyList<DataRepository> ImportCsvFromConfiguration(ImporterConfiguration configuration, string dataPath, char columnSeparator);
      MappingDataFormatParameter GetTime(ImporterConfiguration configuration);
      MappingDataFormatParameter GetMeasurement(ImporterConfiguration configuration);
      MappingDataFormatParameter GetError(ImporterConfiguration configuration);
      void AddError(ImporterConfiguration configuration);
      void RemoveError(ImporterConfiguration configuration);
      void SetIsUnitFromColumn(MappingDataFormatParameter parameter, bool isUnitFromColumn);
      string[] GetAllGroupingColumns(ImporterConfiguration configuration);
      void AddGroupingColumn(ImporterConfiguration configuration, string columnName);
      void RemoveGroupingColumn(ImporterConfiguration configuration, string columnName);
      string[] GetAllLoadedSheets(ImporterConfiguration configuration);
      void SetAllLoadedSheet(ImporterConfiguration configuration, string[] sheets);
      void SetAllLoadedSheet(ImporterConfiguration configuration, string sheet);
      bool IgnoreSheetNamesAtImport { get; set; }
   }

   public class DataImporterTask : IDataImporterTask
   {
      private readonly IDataImporter _dataImporter;
      private readonly IReadOnlyList<MetaDataCategory> _metaDataCategories;
      private readonly DataImporterSettings _dataImporterSettings;
      private readonly IReadOnlyList<ColumnInfo> _columnInfos;
      private readonly IDimensionFactory _dimensionFactory;
      private readonly IPKMLPersistor _pkmlPersistor;
      private readonly ICsvDynamicSeparatorSelector _csvSeparatorSelector;

      public DataImporterTask(
         IDataImporter dataImporter,
         ICsvDynamicSeparatorSelector csvSeparatorSelector,
         IDimensionFactory dimensionFactory,
         IPKMLPersistor pkmlPersistor
      )
      {
         _dataImporter = dataImporter;
         _dimensionFactory = dimensionFactory;
         _pkmlPersistor = pkmlPersistor;
         _metaDataCategories = (IReadOnlyList<MetaDataCategory>) _dataImporter.DefaultMetaDataCategories();
         _dataImporterSettings = new DataImporterSettings
         {
            NameOfMetaDataHoldingMoleculeInformation = Constants.ObservedData.MOLECULE,
            NameOfMetaDataHoldingMolecularWeightInformation = Constants.ObservedData.MOLECULAR_WEIGHT,
            IgnoreSheetNamesAtImport = true
         };
         _columnInfos = ((DataImporter) _dataImporter).DefaultPKSimImportConfiguration();
         _csvSeparatorSelector = csvSeparatorSelector;
      }

      public bool IgnoreSheetNamesAtImport
      {
         get => _dataImporterSettings.IgnoreSheetNamesAtImport;
         set => _dataImporterSettings.IgnoreSheetNamesAtImport = value;
      }

      public IReadOnlyList<DataRepository> ImportExcelFromConfiguration(
         string configurationPath,
         string dataPath)
      {
         return _dataImporter.ImportFromConfiguration(
            GetConfiguration(configurationPath),
            _metaDataCategories,
            _columnInfos,
            _dataImporterSettings,
            dataPath
         );
      }

      public IReadOnlyList<DataRepository> ImportExcelFromConfiguration(
         ImporterConfiguration configuration,
         string dataPath)
      {
         return _dataImporter.ImportFromConfiguration(
            configuration,
            _metaDataCategories,
            _columnInfos,
            _dataImporterSettings,
            dataPath
         );
      }

      public IReadOnlyList<DataRepository> ImportCsvFromConfiguration(
         string configurationPath,
         string dataPath,
         char columnSeparator)
      {
         _csvSeparatorSelector.CsvSeparator = columnSeparator;
         return _dataImporter.ImportFromConfiguration(
            GetConfiguration(configurationPath),
            _metaDataCategories,
            _columnInfos,
            _dataImporterSettings,
            dataPath
         );
      }

      public IReadOnlyList<DataRepository> ImportCsvFromConfiguration(
         ImporterConfiguration configuration,
         string dataPath,
         char columnSeparator)
      {
         _csvSeparatorSelector.CsvSeparator = columnSeparator;
         return _dataImporter.ImportFromConfiguration(
            configuration,
            _metaDataCategories,
            _columnInfos,
            _dataImporterSettings,
            dataPath
         );
      }

      public ImporterConfiguration GetConfiguration(string filePath)
      {
         return _pkmlPersistor.Load<ImporterConfiguration>(filePath);
      }

      /// <summary>
      ///    Creates an empty configuration with the columns "Time" and "Measurement".
      /// </summary>
      /// <returns>A new configuration object</returns>
      public ImporterConfiguration CreateConfiguration()
      {
         var configuration = new ImporterConfiguration();

         var dimension = _dimensionFactory.Dimension(Constants.Dimension.TIME);
         var timeColumn = new Column
         {
            Name = _columnInfos.First(ci => ci.IsBase()).DisplayName,
            Dimension = dimension,
            Unit = new UnitDescription(dimension.DefaultUnitName)
         };
         configuration.AddParameter(new MappingDataFormatParameter(Constants.TIME, timeColumn));

         dimension = _dimensionFactory.Dimension(Constants.Dimension.MOLAR_CONCENTRATION);
         var measurementColumn = new Column
         {
            Name = _columnInfos.First(ci => !(ci.IsAuxiliary() || ci.IsBase())).DisplayName,
            Dimension = dimension,
            Unit = new UnitDescription(dimension.DefaultUnitName)
         };
         configuration.AddParameter(new MappingDataFormatParameter(Constants.MEASUREMENT, measurementColumn));

         return configuration;
      }

      public ImporterConfiguration CreateConfigurationFor(string dataPath)
      {
         return _dataImporter.ConfigurationFromData(dataPath, _columnInfos, _metaDataCategories);
      }

      public void SaveConfiguration(ImporterConfiguration configuration, string path)
      {
         _pkmlPersistor.SaveToPKML(configuration, path);
      }

      /// <summary>
      ///    Add an error column to the configuration if no error column is present.
      ///    If the configuration already has an error column, the method does nothing.
      /// </summary>
      /// <param name="configuration">Configuration object</param>
      public void AddError(ImporterConfiguration configuration)
      {
         //Add a new error column only of the configuration does not have an error column yet
         var errorParameter = GetError(configuration);
         if (errorParameter != null)
            return;

         var measurementUnitDescription = GetMeasurement(configuration).MappedColumn.Unit;
         var errorColumn = new Column
         {
            Name = _columnInfos.First(ci => ci.IsAuxiliary()).DisplayName,
            Dimension = _dimensionFactory.Dimension(Constants.Dimension.MOLAR_CONCENTRATION),
            Unit = new UnitDescription(measurementUnitDescription.SelectedUnit, measurementUnitDescription.ColumnName),
            ErrorStdDev = Constants.STD_DEV_ARITHMETIC
         };
         configuration.AddParameter(new MappingDataFormatParameter(Constants.ERROR, errorColumn));
      }

      public void AddGroupingColumn(ImporterConfiguration configuration, string columnName)
      {
         if (configuration.Parameters.Any(p => p.ColumnName == columnName))
            return;

         DataFormatParameter parameter = new GroupByDataFormatParameter(columnName);
         configuration.AddParameter(parameter);
      }

      public void SetAllLoadedSheet(ImporterConfiguration configuration, string[] sheets)
      {
         configuration.ClearLoadedSheets();
         sheets.Each(configuration.AddToLoadedSheets);
      }

      public void SetAllLoadedSheet(ImporterConfiguration configuration, string sheet)
      {
         SetAllLoadedSheet(configuration, new[] {sheet});
      }

      public string[] GetAllGroupingColumns(ImporterConfiguration configuration)
      {
         return configuration.Parameters
            .Where(p => (p is MetaDataFormatParameter) || (p is GroupByDataFormatParameter))
            .Select(p => p.ColumnName).ToArray();
      }

      public string[] GetAllLoadedSheets(ImporterConfiguration configuration)
      {
         return configuration.LoadedSheets.ToArray();
      }

      public MappingDataFormatParameter GetError(ImporterConfiguration configuration)
      {
         return configuration.Parameters
            .OfType<MappingDataFormatParameter>()
            .FirstOrDefault(p => _columnInfos.First(ci => ci.DisplayName == p.MappedColumn.Name).IsAuxiliary());
      }

      public MappingDataFormatParameter GetMeasurement(ImporterConfiguration configuration)
      {
         return configuration.Parameters.OfType<MappingDataFormatParameter>().FirstOrDefault(p =>
         {
            var columnInfo = _columnInfos.First(ci => ci.DisplayName == p.MappedColumn.Name);
            return !(columnInfo.IsAuxiliary() || columnInfo.IsBase());
         });
      }

      public MappingDataFormatParameter GetTime(ImporterConfiguration configuration)
      {
         return configuration.Parameters.OfType<MappingDataFormatParameter>().FirstOrDefault(p => _columnInfos.First(ci => ci.DisplayName == p.MappedColumn.Name).IsBase());
      }

      public void RemoveError(ImporterConfiguration configuration)
      {
         var errorParameter = GetError(configuration);
         if (errorParameter != null)
            configuration.Parameters.Remove(errorParameter);
      }

      public void RemoveGroupingColumn(ImporterConfiguration configuration, string columnName)
      {
         var column = configuration.Parameters.FirstOrDefault(p => p.ColumnName == columnName);
         if (column == null)
            return;
         configuration.Parameters.Remove(column);
      }

      public void SetIsUnitFromColumn(MappingDataFormatParameter parameter, bool isUnitFromColumn)
      {
         if (isUnitFromColumn)
         {
            var oldUnit = parameter.MappedColumn.Unit.SelectedUnit;
            parameter.MappedColumn.Unit = new UnitDescription(oldUnit, oldUnit);
            parameter.MappedColumn.Dimension = null;
         }
         else
         {
            var oldUnit = parameter.MappedColumn.Unit.SelectedUnit;
            parameter.MappedColumn.Unit = new UnitDescription(oldUnit);
            parameter.MappedColumn.Dimension = _dimensionFactory.DimensionForUnit(oldUnit);
         }
      }
   }
}