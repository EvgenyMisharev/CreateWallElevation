using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreateWallElevation
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    class CreateWallElevationCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;

            //Вызов формы
            CreateWallElevationWPF createWallElevationWPF = new CreateWallElevationWPF(doc);
            createWallElevationWPF.ShowDialog();
            if (createWallElevationWPF.DialogResult != true)
            {
                return Result.Cancelled;
            }

            return Result.Succeeded;
        }
    }
}
