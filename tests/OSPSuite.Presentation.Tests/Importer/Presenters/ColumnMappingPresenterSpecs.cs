﻿using FakeItEasy;
using NUnit.Framework;
using OSPSuite.BDDHelper;
using System.Linq;
using OSPSuite.Infrastructure.Import.Core;
using OSPSuite.Infrastructure.Import.Services;
using OSPSuite.Presentation.Presenters.Importer;
using OSPSuite.Presentation.Views.Importer;
using OSPSuite.Core.Import;
using System.Collections.Generic;
using OSPSuite.Core.Domain;
using OSPSuite.Core.Domain.UnitSystem;
using OSPSuite.BDDHelper.Extensions;
using OSPSuite.Helpers;

namespace OSPSuite.Presentation.Importer.Presenters 
{
   public abstract class concern_for_ColumnMappingPresenter : ContextSpecification<ColumnMappingPresenter>
   {
      protected IDataFormat _basicFormat;
      protected IColumnMappingView _view;
      protected IImporter _importer;
      protected IDimensionFactory _dimensionFactory;
      protected IMappingParameterEditorPresenter _mappingParameterEditorPresenter;
      protected IMetaDataParameterEditorPresenter _metaDataParameterEditorPresenter;
      protected IReadOnlyList<ColumnInfo> _columnInfos;
      protected IReadOnlyList<MetaDataCategory> _metaDataCategories;
      protected List<DataFormatParameter> _parameters = new List<DataFormatParameter>() 
      {
         new MappingDataFormatParameter("Time", new Column() { Name = "Time", Unit = new UnitDescription("min") }),
         new MappingDataFormatParameter("Observation", new Column() { Name = "Concentration", Unit = new UnitDescription("mol/l") }),
         new MappingDataFormatParameter("Error", new Column() { Name = "Error", Unit = new UnitDescription("?", "") }),
         new GroupByDataFormatParameter("Study id")
      };

      public override void GlobalContext()
      {
         base.GlobalContext();
         _basicFormat = A.Fake<IDataFormat>();
         A.CallTo(() => _basicFormat.Parameters).Returns(_parameters);
         _view = A.Fake<IColumnMappingView>();
         _importer = A.Fake<IImporter>();
         _dimensionFactory = A.Fake<IDimensionFactory>();
         A.CallTo(() => _importer.CheckWhetherAllDataColumnsAreMapped(A<IReadOnlyList<ColumnInfo>>.Ignored,
            A<IEnumerable<DataFormatParameter>>.Ignored)).Returns(new MappingProblem()
            {MissingMapping = new List<string>(), MissingUnit = new List<string>()});
      }

      protected void UpdateSettings()
      {
         sut.SetSettings(_metaDataCategories, _columnInfos);
         sut.SetDataFormat(_basicFormat);
      }

      protected override void Context()
      {
         base.Context();
         _columnInfos = new List<ColumnInfo>()
         {
            new ColumnInfo() { Name = "Time", IsMandatory = true, BaseGridName = "Time" },
            new ColumnInfo() { Name = "Concentration", IsMandatory = true, BaseGridName = "Time" },
            new ColumnInfo() { Name = "Error", IsMandatory = false, RelatedColumnOf = "Concentration", BaseGridName = "Time"}
         };
         _columnInfos[2].SupportedDimensions.Add(DomainHelperForSpecs.ConcentrationDimensionForSpecs());
         _metaDataCategories = new List<MetaDataCategory>()
         {
            new MetaDataCategory()
            {
               Name = "Time",
               IsMandatory = true,
            },
            new MetaDataCategory()
            {
               Name = "Concentration",
               IsMandatory = true
            },
            new MetaDataCategory()
            {
               DisplayName = "Error",
               IsMandatory = false
            },
            new MetaDataCategory()
            {
               Name = "Molecule",
               IsMandatory = false,
               AllowsManualInput = true
            }
         };
         _mappingParameterEditorPresenter = A.Fake<IMappingParameterEditorPresenter>();
         _metaDataParameterEditorPresenter = A.Fake<IMetaDataParameterEditorPresenter>();
         sut = new ColumnMappingPresenter(_view, _importer, _mappingParameterEditorPresenter, _metaDataParameterEditorPresenter, _dimensionFactory);
      }
   }

   public class When_setting_data_format : concern_for_ColumnMappingPresenter
   {
      protected override void Context()
      {
         base.Context();
         UpdateSettings();
      }

      [Observation]
      public void identify_basic_format()
      {
         A.CallTo(
            () => _view.SetMappingSource(
               A<IList<ColumnMappingDTO>>.That.Matches(l => 
                  l.Count(m => m.CurrentColumnType == ColumnMappingDTO.ColumnType.Mapping && m.Source is MappingDataFormatParameter && (m.Source as MappingDataFormatParameter).MappedColumn.Name == "Time") == 1 &&
                  l.Count(m => m.CurrentColumnType == ColumnMappingDTO.ColumnType.Mapping && m.Source is MappingDataFormatParameter && (m.Source as MappingDataFormatParameter).MappedColumn.Name == "Concentration") == 1 &&
                  l.Count(m => m.CurrentColumnType == ColumnMappingDTO.ColumnType.GroupBy && m.Source is GroupByDataFormatParameter && (m.Source as GroupByDataFormatParameter).ColumnName == "Study id") == 1
            ))).MustHaveHappened();
      }
   }

   public class When_initializing_error_unit : concern_for_ColumnMappingPresenter
   {
      protected override void Context()
      {
         base.Context();
         UpdateSettings();
      }

      protected override void Because()
      {
         sut.InitializeErrorUnit();
      }

      [Observation]
      public void the_unit_is_properly_set()
      {
         _basicFormat.Parameters.OfType<MappingDataFormatParameter>().First(p => p.ColumnName == "Observation").MappedColumn.Unit.ShouldBeEqualTo(_basicFormat.Parameters.OfType<MappingDataFormatParameter>().First(p => p.ColumnName == "Error").MappedColumn.Unit);
      }
   }

   public class When_initializing_error_unit_on_initialized_error : concern_for_ColumnMappingPresenter
   {
      protected override void Context()
      {
         base.Context();
         A.CallTo(() => _basicFormat.Parameters).Returns(new List<DataFormatParameter>() 
         {
            new MappingDataFormatParameter("Time", new Column() { Name = "Time", Unit = new UnitDescription("min") }),
            new MappingDataFormatParameter("Observation", new Column() { Name = "Concentration", Unit = new UnitDescription("mol/l") }),
            new MappingDataFormatParameter("Error", new Column() { Name = "Error", Unit = new UnitDescription("g/l"), ErrorStdDev = Constants.STD_DEV_GEOMETRIC }),
            new GroupByDataFormatParameter("Study id")
         });
         UpdateSettings();
      }

      protected override void Because()
      {
         sut.InitializeErrorUnit();
      }

      [Observation]
      public void the_unit_is_properly_set()
      {
         _basicFormat.Parameters.OfType<MappingDataFormatParameter>().First(p => p.ColumnName == "Error").MappedColumn.Unit.SelectedUnit.ShouldBeEmpty();
      }
   }

   public class When_updating_description_for_model_with_first_error_type : concern_for_ColumnMappingPresenter
   {
      protected override void Context()
      {
         base.Context();
         A.CallTo(() => _mappingParameterEditorPresenter.Unit).Returns(new UnitDescription(""));
         A.CallTo(() => _mappingParameterEditorPresenter.SelectedErrorType).Returns(0);
         UpdateSettings();
      }

      protected override void Because()
      {
         sut.SetSubEditorSettingsForMapping(new ColumnMappingDTO
         (
            ColumnMappingDTO.ColumnType.Mapping, 
            "Error", 
            _parameters[2],
            0,
            _columnInfos[2]
         ));
         sut.UpdateDescriptionForModel(_parameters[2] as MappingDataFormatParameter);
      }

      [Observation]
      public void the_ErrorStdDev_is_properly_set()
      {
         (_basicFormat.Parameters[2] as MappingDataFormatParameter).MappedColumn.ErrorStdDev.ShouldBeEqualTo(Constants.STD_DEV_ARITHMETIC);
      }
   }

   public class When_updating_description_for_model_with_second_error_type : concern_for_ColumnMappingPresenter
   {
      protected override void Context()
      {
         base.Context();
         A.CallTo(() => _mappingParameterEditorPresenter.Unit).Returns(new UnitDescription(""));
         A.CallTo(() => _mappingParameterEditorPresenter.SelectedErrorType).Returns(1);
         UpdateSettings();
      }

      protected override void Because()
      {
         sut.SetSubEditorSettingsForMapping(new ColumnMappingDTO
         (
            ColumnMappingDTO.ColumnType.Mapping,
            "Error",
            _parameters[2],
            0,
            _columnInfos[2]
         ));
         sut.UpdateDescriptionForModel(_parameters[2] as MappingDataFormatParameter);
      }

      [Observation]
      public void the_ErrorStdDev_is_properly_set()
      {
         (_basicFormat.Parameters[2] as MappingDataFormatParameter).MappedColumn.ErrorStdDev.ShouldBeEqualTo(Constants.STD_DEV_GEOMETRIC);
      }
   }

   public class When_updating_description_for_model_for_observation : concern_for_ColumnMappingPresenter
   {
      protected override void Context()
      {
         base.Context();
         A.CallTo(() => _mappingParameterEditorPresenter.Unit).Returns(new UnitDescription(""));
         A.CallTo(() => _mappingParameterEditorPresenter.LloqColumn).Returns("Col1");
         A.CallTo(() => _mappingParameterEditorPresenter.LloqFromColumn()).Returns(true);
         A.CallTo(() => _basicFormat.ExcelColumnNames).Returns(new List<string>() { "Time", "Observation", "Error", "Col1", "Col2" });
         UpdateSettings();
      }

      protected override void Because()
      {
         sut.SetSubEditorSettingsForMapping(new ColumnMappingDTO
         (
            ColumnMappingDTO.ColumnType.Mapping,
            "Error",
            _parameters[1],
            0,
            _columnInfos[1]
         ));
         sut.UpdateDescriptionForModel(_parameters[1] as MappingDataFormatParameter);
      }

      [Observation]
      public void the_lloq_is_properly_set()
      {
         (_basicFormat.Parameters[1] as MappingDataFormatParameter).MappedColumn.LloqColumn.ShouldBeEqualTo("Col1");
      }
   }

   public class When_clearing_description_for_meta_data_model : concern_for_ColumnMappingPresenter
   {
      protected ColumnMappingDTO _model;
      protected override void Context()
      {
         base.Context();
         UpdateSettings();
      }

      protected override void Because()
      {
         _model = new ColumnMappingDTO
         (
            ColumnMappingDTO.ColumnType.MetaData,
            "Molecule",
            new MetaDataFormatParameter("Col1", "Molecule"),
            0
         );
         sut.ClearRow(_model);
      }

      [Observation]
      public void the_column_is_cleared()
      {
         string.IsNullOrEmpty((_model.Source as MetaDataFormatParameter).ColumnName).ShouldBeTrue();
      }
   }

   public class When_setting_editor_settings_for_mapping : concern_for_ColumnMappingPresenter
   {
      protected override void Context()
      {
         base.Context();
         A.CallTo(() => _basicFormat.ExcelColumnNames).Returns(new List<string>() { "Time", "Concentration", "Error", "Col1", "Col2" });
         UpdateSettings();
      }

      protected override void Because()
      {
         sut.SetSubEditorSettingsForMapping(new ColumnMappingDTO
         (
            ColumnMappingDTO.ColumnType.Mapping,
            "Concentration",
            _parameters[1],
            0,
            _columnInfos[1]
         ));
      }

      [Observation]
      public void the_units_are_properly_set()
      {
         A.CallTo(() => _mappingParameterEditorPresenter.SetUnitOptions
         (
            A<Column>.That.Matches(c => c.Name == "Concentration"), 
            A<IReadOnlyList<IDimension>>.Ignored, 
            A<IEnumerable<string>>.That.Matches(l => l.Contains("Col1") && l.Contains("Col2"))
         )).MustHaveHappened();
      }

      [Observation]
      public void the_lloq_is_properly_set()
      {
         A.CallTo(() => _mappingParameterEditorPresenter.SetLloqOptions
         (
            A<IEnumerable<string>>.That.Matches(l => l.Contains("Col1") && l.Contains("Col2")),
            A<string>.Ignored,
            false
         )).MustHaveHappened();
      }
   }

   public class When_unit_is_manually_set :concern_for_ColumnMappingPresenter
   {
      protected MappingDataFormatParameter _mappingSource;

      protected override void Context()
      {
         base.Context();
         _mappingSource = _parameters[2] as MappingDataFormatParameter;
         A.CallTo(() => _mappingParameterEditorPresenter.Unit).Returns(new UnitDescription("µmol/l"));
         A.CallTo(() => _mappingParameterEditorPresenter.SelectedErrorType).Returns(1);
         UpdateSettings();
      }

      protected override void Because()
      {
         sut.UpdateDescriptionForModel(_mappingSource);
      }

      [Observation]
      public void the_dimension_is_set()
      {
         _mappingSource.MappedColumn.Dimension.ShouldNotBeNull();
      }
   }

   public class When_setting_editor_settings_for_error : concern_for_ColumnMappingPresenter
   {
      protected override void Context()
      {
         base.Context();
         A.CallTo(() => _basicFormat.ExcelColumnNames).Returns(new List<string>() { "Time", "Concentration", "Error", "Col1", "Col2" });
         UpdateSettings();
      }

      protected override void Because()
      {
         sut.SetSubEditorSettingsForMapping(new ColumnMappingDTO
         (
            ColumnMappingDTO.ColumnType.Mapping,
            "Error",
            _parameters[2],
            0,
            _columnInfos[2]
         ));
      }

      [Observation]
      public void the_errors_are_properly_set()
      {
         A.CallTo(() => _mappingParameterEditorPresenter.SetErrorTypeOptions
         (
            A<IEnumerable<string>>.That.Matches(l => l.Contains(Constants.STD_DEV_ARITHMETIC) && l.Contains(Constants.STD_DEV_GEOMETRIC)),
            A<string>.Ignored
         )).MustHaveHappened();
      }
   }

   public class When_getting_available_rows_for : concern_for_ColumnMappingPresenter
   {
      protected override void Context()
      {
         base.Context();
         A.CallTo(() => _basicFormat.ExcelColumnNames).Returns(new List<string>() { "Time", "Concentration", "Error", "Col1", "Col2" });
         UpdateSettings();
      }

      [Observation]
      public void the_rows_for_are_properly_populated()
      {
         var res = sut.GetAvailableRowsFor(new ColumnMappingDTO
         (
            ColumnMappingDTO.ColumnType.Mapping,
            "Error",
            _parameters[2],
            0,
            _columnInfos[2]
         ));
         (res.Any(r => r.Description == "Col1") && res.Any(r => r.Description == "Col2") && res.Any(r => r.Description == "Error") && res.Any(r => r.Description == "Concentration")).ShouldBeTrue();
      }

      [Observation]
      public void the_options_for_are_properly_populated()
      {
         var res = sut.GetAvailableOptionsFor(new ColumnMappingDTO
         (
            ColumnMappingDTO.ColumnType.Mapping,
            "Error",
            _parameters[2],
            0,
            _columnInfos[2]
         ));
         (res.Any(r => r.Label.StartsWith("Col1")) && res.Any( c => c.Label.StartsWith("Col2")) && res.Any(r => r.Label.StartsWith("Error")) && res.Any(r => r.Label.StartsWith("Concentration"))).ShouldBeTrue();
      }
   }

   public class When_setting_mapping_column : concern_for_ColumnMappingPresenter
   {
      protected ColumnMappingDTO _model;

      [TestCase("mmol/l", "?", true, false)]
      [TestCase("mmol/l", "mol/l", true, true)]
      [TestCase("mmol/l", "?", false, true)]
      [TestCase("mmol/l", "mol/l", false, true)]
      public void the_unit_is_set_properly(string oldUnitDescription, string newUnitDescription, bool haveOldSource, bool shouldUpdate)
      {
         //Set up
         UpdateSettings();
         MappingDataFormatParameter mappingSource = null;
         if (haveOldSource)
         {
            mappingSource = _parameters[2] as MappingDataFormatParameter;
            mappingSource.MappedColumn.Unit = new UnitDescription(oldUnitDescription);
         }
         _model = new ColumnMappingDTO(ColumnMappingDTO.ColumnType.Mapping, "Concentration", mappingSource, 0);
         A.CallTo(() => _basicFormat.ExtractUnitDescriptions(A<string>.Ignored, A<IReadOnlyList<IDimension>>.Ignored)).Returns(new UnitDescription(newUnitDescription));

         //Act
         _model.ExcelColumn = "Measurement";
         sut.SetDescriptionForRow(_model);

         //Assert
         (_model.Source as MappingDataFormatParameter).MappedColumn.Unit.SelectedUnit.ShouldBeEqualTo(shouldUpdate ? newUnitDescription : oldUnitDescription);
      }
   }
}
