﻿using System.Collections.Generic;
using System.Linq;
using OSPSuite.Core.Domain;
using OSPSuite.Core.Domain.UnitSystem;
using OSPSuite.Core.Import;
using OSPSuite.Presentation.Views.Importer;
using OSPSuite.Utility.Extensions;

namespace OSPSuite.Presentation.Presenters.Importer
{
   public class UnitsEditorPresenter : AbstractDisposablePresenter<IUnitsEditorView, IUnitsEditorPresenter>, IUnitsEditorPresenter
   {
      private Column _importDataColumn;
      private IReadOnlyList<IDimension> _dimensions;
      private string _selectedUnit;
      private string _selectedColumn;
      public IDimension Dimension { get; private set; }

      private bool _columnMapping;

      public UnitDescription Unit => new UnitDescription(_selectedUnit, _selectedColumn);

      public UnitsEditorPresenter(IUnitsEditorView view) : base(view)
      {
      }

      public void SetOptions(Column importDataColumn, IReadOnlyList<IDimension> dimensions, IEnumerable<string> availableColumns)
      {
         _importDataColumn = importDataColumn;
         _dimensions = dimensions;

         _columnMapping = !importDataColumn.Unit.ColumnName.IsNullOrEmpty();
         Dimension = importDataColumn.Dimension;
         if (Dimension != null && !_dimensions.Contains(Dimension))
            Dimension = _dimensions.FirstOrDefault();

         _selectedUnit = importDataColumn.Unit.SelectedUnit;
         FillDimensions(importDataColumn.Unit.SelectedUnit);

         View.FillColumnComboBox(availableColumns);
         View.SetParams(_columnMapping, useDimensionSelector);
      }

      public void SelectDimension(string dimensionName)
      {
         this.DoWithinExceptionHandler(() =>
         {
            Dimension = _dimensions.FirstOrDefault(d => string.Equals(d.Name, dimensionName)) ??
                        _dimensions.FirstOrDefault() ??
                        Constants.Dimension.NO_DIMENSION;
            //checking whether _selectedUnit is not supported by the current dimension, meaning that the user 
            //has selected a new dimension and the unit must be reset to the default unit of this dimension
            if (_selectedUnit == null || !Dimension.HasUnit(_selectedUnit))
               _selectedUnit = Dimension.DefaultUnitName;
            
            SetUnit();
            fillUnits(_selectedUnit);
         });
      }

      public void SelectColumn(string column)
      {
         this.DoWithinExceptionHandler(() =>
         {
            _columnMapping = true;
            _selectedColumn = column;
         });
      }

      public void SetUnitColumnSelection()
      {
         View.SetUnitColumnSelection();
      }

      public void SetUnitsManualSelection()
      {
         View.SetUnitsManualSelection();
      }

      public void ShowColumnToggle()
      {
         View.ShowToggle();
      }

      public void SelectUnit(string unit)
      {
         this.DoWithinExceptionHandler(() =>
         {
            _columnMapping = false;
            _selectedColumn = null;
            _selectedUnit = unit;
         });
      }

      private bool useDimensionSelector => _dimensions.Count > 1;

      public void FillDimensions(string selectedUnit)
      {
         if (useDimensionSelector && Dimension != null)
            View.FillDimensionComboBox(_dimensions, Dimension.Name);
         else if (Dimension == null && selectedUnit == UnitDescription.InvalidUnit)
            View.FillDimensionComboBox(_dimensions, _dimensions.FirstOrDefault()?.Name);
         else
            View.FillDimensionComboBox(_dimensions, _dimensions.FirstOrDefault()?.Name ?? "");
      }

      private void fillUnits(string selectedUnit)
      {
         if (Dimension != null)
            View.FillUnitComboBox(Dimension.Units, selectedUnit);
      }

      public void SetUnit()
      {
         this.DoWithinExceptionHandler(() =>
         {
            _importDataColumn.Unit = new UnitDescription(_selectedUnit);
            _importDataColumn.Dimension = Dimension;
         });
      }
   }
}