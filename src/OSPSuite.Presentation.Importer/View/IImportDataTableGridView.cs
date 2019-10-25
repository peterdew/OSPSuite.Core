using System.Collections.Generic;
using OSPSuite.Core.Importer;
using OSPSuite.Presentation.Importer.Presenter;
using OSPSuite.Presentation.Views;

namespace OSPSuite.Presentation.Importer.View
{
   public interface IImportDataTableGridView : IView<IImportDataTableGridPresenter>
   {
      void BindTo(ImportDataTable table);
      void ReflectMetaDataChangesForColumn(ImportDataColumn column);

      /// <summary>
      /// This method sets the image of the given grid column depending on fill state and requirements.
      /// </summary>
      void SetColumnImage(string column);

      void Clear();
      void SetUnitForColumn(ImportDataTable table);
      void SetInputParametersForColumn(IList<InputParameter> inputParameters, Dimension dimension, string columnName);
      void SetUnitInformationForColumn(Dimension dimension, Unit unit, string columnName);
   }
}