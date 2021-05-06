using OSPSuite.Infrastructure.Import.Services;
using OSPSuite.Presentation.Core;
using OSPSuite.Presentation.Presenters.Importer;

namespace OSPSuite.UI.Services
{
   public class CsvSeparatorSelector : ICsvSeparatorSelector
   {
      private readonly IApplicationController _applicationController;

      public CsvSeparatorSelector(IApplicationController applicationController)
      {
         _applicationController = applicationController;
      }

      public char? GetCsvSeparator(string fileName)
      {
         using (var csvSeparatorPresenter = _applicationController.Start<ICsvSeparatorSelectorPresenter>())
         {
            csvSeparatorPresenter.SetFileName(fileName);

            return csvSeparatorPresenter.Canceled() ? null : csvSeparatorPresenter.GetCsvSeparator();
         }
      }
   }
}

