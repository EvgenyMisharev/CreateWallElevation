using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
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
            Selection sel = commandData.Application.ActiveUIDocument.Selection;

            List<ViewSheet> viewSheetList = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Sheets)
                .WhereElementIsNotElementType()
                .Cast<ViewSheet>()
                .OrderBy(vs => vs.SheetNumber, new AlphanumComparatorFastString())
                .ToList();

            //Вызов формы
            CreateWallElevationWPF createWallElevationWPF = new CreateWallElevationWPF(doc, viewSheetList);
            createWallElevationWPF.ShowDialog();
            if (createWallElevationWPF.DialogResult != true)
            {
                return Result.Cancelled;
            }

            ViewFamilyType selectedViewFamilyType = createWallElevationWPF.SelectedViewFamilyType;
            string selectedBuildByName = createWallElevationWPF.SelectedBuildByName;
            string selectedUseToBuildName = createWallElevationWPF.SelectedUseToBuildName;

            double indent = createWallElevationWPF.Indent;
            double indentUp = createWallElevationWPF.IndentUp;
            double indentDown = createWallElevationWPF.IndentDown;
            double projectionDepth = createWallElevationWPF.ProjectionDepth;

            bool useTemplate = createWallElevationWPF.UseTemplate;
            ViewSection viewSectionTemplate = createWallElevationWPF.ViewSectionTemplate;
            int curveNumberOfSegments = createWallElevationWPF.CurveNumberOfSegments;
            ViewSheet selectedViewSheet = createWallElevationWPF.SelectedViewSheet;

            if (projectionDepth == 0)
            {
                TaskDialog.Show("Revit", "В параметр \"Глубина проекции\" введено недопустимое значение! Будет использовано значение по умолчанию!");
#if R2019 || R2020 || R2021
                projectionDepth = UnitUtils.ConvertToInternalUnits(500, DisplayUnitType.DUT_MILLIMETERS);
#else
                projectionDepth = UnitUtils.ConvertToInternalUnits(500, UnitTypeId.Millimeters);
#endif
            }

            using (Transaction t = new Transaction(doc))
            {
                t.Start("Развертка стен");
                Wall wall = null;
                XYZ pickedPoint = null;

                if (selectedBuildByName == "rbt_ByRoom")
                {
                    List<Room> roomList = new List<Room>();
                    roomList = GetRoomsFromCurrentSelection(doc, sel);
                    if (roomList.Count == 0)
                    {
                        RoomSelectionFilter selFilter = new RoomSelectionFilter();
                        IList<Reference> selRooms = null;
                        try
                        {
                            selRooms = sel.PickObjects(ObjectType.Element, selFilter, "Выберите помещения!");
                        }
                        catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                        {
                            return Result.Cancelled;
                        }

                        foreach (Reference roomRef in selRooms)
                        {
                            roomList.Add(doc.GetElement(roomRef) as Room);
                        }

                        roomList = roomList.OrderBy(r => r.Number, new AlphanumComparatorFastString()).ThenBy(r => r.Name, new AlphanumComparatorFastString()).ToList();
                    }

                    if (roomList.Count != 0)
                    {
                        double additionalOffset = 0;

                        foreach (Room room in roomList)
                        {
                            int cnt = 1;
                            List<ViewSection> viewSectionsList = new List<ViewSection>();
                            List<Curve> tmpRoomCurves = new List<Curve>();
                            IList<IList<BoundarySegment>> loops = room.GetBoundarySegments(new SpatialElementBoundaryOptions());
                            foreach (BoundarySegment seg in loops.First())
                            {
                                tmpRoomCurves.Add(seg.GetCurve());
                            }

                            List<Curve> roomCurves = new List<Curve>();
                            Curve tmpStartCurve = null;
                            Curve tmpCurve = null;
                            XYZ tmpCurveStartPoint = null;
                            XYZ tmpCurveEndPoint = null;

                            foreach (Curve curve in tmpRoomCurves)
                            {
                                if (tmpCurve == null)
                                {
                                    tmpStartCurve = curve;
                                    tmpCurve = curve;
                                    tmpCurveStartPoint = curve.GetEndPoint(0);
                                    tmpCurveEndPoint = curve.GetEndPoint(1);
                                }
                                else
                                {
                                    if(curve is Arc)
                                    {
                                        if (tmpRoomCurves.IndexOf(curve) != tmpRoomCurves.Count - 1)
                                        {
                                            roomCurves.Add(tmpCurve);
                                            tmpCurve = curve;
                                            tmpCurveStartPoint = curve.GetEndPoint(0);
                                            tmpCurveEndPoint = curve.GetEndPoint(1);
                                        }
                                        else
                                        {
                                            roomCurves.Add(tmpCurve);
                                            roomCurves.Add(curve);
                                        }

                                    }
                                    else
                                    {
                                        if(tmpCurve is Arc)
                                        {
                                            if (tmpRoomCurves.IndexOf(curve) != tmpRoomCurves.Count - 1)
                                            {
                                                roomCurves.Add(tmpCurve);
                                                tmpCurve = curve;
                                                tmpCurveStartPoint = curve.GetEndPoint(0);
                                                tmpCurveEndPoint = curve.GetEndPoint(1);
                                            }
                                            else
                                            {
                                                roomCurves.Add(tmpCurve);
                                                roomCurves.Add(curve);
                                            }
                                        }
                                        else
                                        {
                                            if ((curve as Line).Direction.IsAlmostEqualTo((tmpCurve as Line).Direction))
                                            {
                                                if (tmpRoomCurves.IndexOf(curve) != tmpRoomCurves.Count - 1)
                                                {
                                                    tmpCurve = Line.CreateBound(tmpCurveStartPoint, curve.GetEndPoint(1)) as Curve;
                                                    tmpCurveStartPoint = tmpCurve.GetEndPoint(0);
                                                    tmpCurveEndPoint = tmpCurve.GetEndPoint(1);
                                                }
                                                else
                                                {
                                                    if((curve as Line).Direction.IsAlmostEqualTo((tmpStartCurve as Line).Direction))
                                                    {
                                                        tmpCurve = Line.CreateBound(tmpCurveStartPoint, tmpStartCurve.GetEndPoint(1)) as Curve;
                                                        roomCurves.Add(tmpCurve);
                                                        roomCurves.Remove(tmpStartCurve);
                                                    }
                                                    else
                                                    {
                                                        tmpCurve = Line.CreateBound(tmpCurveStartPoint, curve.GetEndPoint(1)) as Curve;
                                                        roomCurves.Add(tmpCurve);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                if (tmpRoomCurves.IndexOf(curve) != tmpRoomCurves.Count - 1)
                                                {
                                                    roomCurves.Add(tmpCurve);
                                                    tmpCurve = curve;
                                                    tmpCurveStartPoint = curve.GetEndPoint(0);
                                                    tmpCurveEndPoint = curve.GetEndPoint(1);
                                                }
                                                else
                                                {
                                                    roomCurves.Add(tmpCurve);
                                                    roomCurves.Add(curve);
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            foreach (Curve curve in roomCurves)
                            {
                                if (selectedUseToBuildName == "rbt_Section")
                                {
                                    if (curve is Line)
                                    {
                                        XYZ start = curve.GetEndPoint(0);
                                        XYZ end = curve.GetEndPoint(1);
                                        XYZ curveDir = (end - start).Normalize();
                                        double w = (end - start).GetLength();

                                        Transform curveTransform = curve.ComputeDerivatives(0.5, true);
                                        XYZ origin = curveTransform.Origin;
                                        XYZ right = curveDir;
                                        XYZ up = XYZ.BasisZ;
                                        XYZ viewdir = curveDir.CrossProduct(up).Normalize();

                                        BoundingBoxXYZ roomBb = room.get_BoundingBox(null);
                                        double minZ = roomBb.Min.Z;
                                        double maxZ = roomBb.Max.Z;

                                        Transform transform = Transform.Identity;
                                        transform.Origin = origin;
                                        transform.BasisX = right;
                                        transform.BasisY = up;
                                        transform.BasisZ = viewdir;

                                        BoundingBoxXYZ sectionBox = new BoundingBoxXYZ();
                                        sectionBox.Transform = transform;
                                        sectionBox.Min = new XYZ(-w / 2, minZ + indentDown, -indent);
                                        sectionBox.Max = new XYZ(w / 2, maxZ + indentUp, projectionDepth - indent);

                                        ViewSection viewSection = ViewSection.CreateSection(doc, selectedViewFamilyType.Id, sectionBox);
                                        viewSection.Name = $"Р_П{room.Number}_{cnt}_{viewSection.Id}";
                                        cnt++;

                                        if (useTemplate)
                                        {
                                            viewSection.get_Parameter(BuiltInParameter.VIEW_TEMPLATE).Set(viewSectionTemplate.Id);
                                        }
                                        viewSectionsList.Add(viewSection);
                                    }
                                    else
                                    {
                                        XYZ center = (curve as Arc).Center;
                                        XYZ start = curve.GetEndPoint(0) + indent * (center - curve.GetEndPoint(0)).Normalize();
                                        XYZ end = curve.GetEndPoint(1) + indent * (center - curve.GetEndPoint(1)).Normalize();

                                        XYZ startVector = (start - center).Normalize();
                                        XYZ endVector = (end - center).Normalize();

                                        double startEndAngle = endVector.AngleTo(startVector);
                                        double anglePerSegment = startEndAngle / curveNumberOfSegments;

                                        XYZ startEndLineCenter = (start + end) / 2;
                                        XYZ v1 = (startEndLineCenter - center).Normalize();
                                        XYZ v2 = (startEndLineCenter - (room.Location as LocationPoint).Point).Normalize();
                                        double angle = v1.AngleTo(v2) * (180 / Math.PI);

                                        if(angle <= 45)
                                        {
                                            start = curve.GetEndPoint(0) + indent * (center - curve.GetEndPoint(0)).Normalize();
                                            end = curve.GetEndPoint(1) + indent * (center - curve.GetEndPoint(1)).Normalize();

                                            List<XYZ> arcPoints = new List<XYZ>() { start };
                                            for (int i = 1; i < curveNumberOfSegments; i++)
                                            {
                                                Transform rotationTransform = Transform.CreateRotationAtPoint(new XYZ(0, 0, 1), i * anglePerSegment, center);
                                                XYZ rotatedVector = rotationTransform.OfPoint(start);
                                                arcPoints.Add(rotatedVector);
                                            }
                                            arcPoints.Add(end);

                                            for (int i = 0; i < arcPoints.Count - 1; i++)
                                            {
                                                XYZ tmpStart = arcPoints[i];
                                                XYZ tmpEnd = arcPoints[i + 1];
                                                XYZ curveDir = (tmpEnd - tmpStart).Normalize();
                                                double w = (tmpEnd - tmpStart).GetLength();

                                                XYZ origin = (tmpStart + tmpEnd) / 2;
                                                XYZ right = curveDir;
                                                XYZ up = XYZ.BasisZ;
                                                XYZ viewdir = curveDir.CrossProduct(up).Normalize();

                                                BoundingBoxXYZ roomBb = room.get_BoundingBox(null);
                                                double minZ = roomBb.Min.Z;
                                                double maxZ = roomBb.Max.Z;

                                                Transform transform = Transform.Identity;
                                                transform.Origin = origin;
                                                transform.BasisX = right;
                                                transform.BasisY = up;
                                                transform.BasisZ = viewdir;

                                                BoundingBoxXYZ sectionBox = new BoundingBoxXYZ();
                                                sectionBox.Transform = transform;
                                                sectionBox.Min = new XYZ(-w / 2, minZ + indentDown, 0);
                                                sectionBox.Max = new XYZ(w / 2, maxZ + indentUp, projectionDepth);

                                                ViewSection viewSection = ViewSection.CreateSection(doc, selectedViewFamilyType.Id, sectionBox);
                                                viewSection.Name = $"Р_П{room.Number}_{cnt}_{viewSection.Id}";
                                                cnt++;

                                                if (useTemplate)
                                                {
                                                    viewSection.get_Parameter(BuiltInParameter.VIEW_TEMPLATE).Set(viewSectionTemplate.Id);
                                                }
                                                viewSectionsList.Add(viewSection);
                                            }
                                        }
                                        else
                                        {
                                            start = curve.GetEndPoint(0) - indent * (center - curve.GetEndPoint(0)).Normalize();
                                            end = curve.GetEndPoint(1) - indent * (center - curve.GetEndPoint(1)).Normalize();

                                            List<XYZ> arcPoints = new List<XYZ>() { start };
                                            for (int i = 1; i < curveNumberOfSegments; i++)
                                            {
                                                Transform rotationTransform = Transform.CreateRotationAtPoint(new XYZ(0, 0, 1), - i * anglePerSegment, center);
                                                XYZ rotatedVector = rotationTransform.OfPoint(start);
                                                arcPoints.Add(rotatedVector);
                                            }
                                            arcPoints.Add(end);

                                            for (int i = 0; i < arcPoints.Count - 1; i++)
                                            {
                                                XYZ tmpStart = arcPoints[i];
                                                XYZ tmpEnd = arcPoints[i + 1];
                                                XYZ curveDir = (tmpEnd - tmpStart).Normalize();
                                                double w = (tmpEnd - tmpStart).GetLength();

                                                XYZ origin = (tmpStart + tmpEnd) / 2;
                                                XYZ right = curveDir;
                                                XYZ up = XYZ.BasisZ;
                                                XYZ viewdir = curveDir.CrossProduct(up).Normalize();

                                                BoundingBoxXYZ roomBb = room.get_BoundingBox(null);
                                                double minZ = roomBb.Min.Z;
                                                double maxZ = roomBb.Max.Z;

                                                Transform transform = Transform.Identity;
                                                transform.Origin = origin;
                                                transform.BasisX = right;
                                                transform.BasisY = up;
                                                transform.BasisZ = viewdir;

                                                BoundingBoxXYZ sectionBox = new BoundingBoxXYZ();
                                                sectionBox.Transform = transform;
                                                sectionBox.Min = new XYZ(-w / 2, minZ + indentDown, 0);
                                                sectionBox.Max = new XYZ(w / 2, maxZ + indentUp, projectionDepth);

                                                ViewSection viewSection = ViewSection.CreateSection(doc, selectedViewFamilyType.Id, sectionBox);
                                                viewSection.Name = $"Р_П{room.Number}_{cnt}_{viewSection.Id}";
                                                cnt++;

                                                if (useTemplate)
                                                {
                                                    viewSection.get_Parameter(BuiltInParameter.VIEW_TEMPLATE).Set(viewSectionTemplate.Id);
                                                }
                                                viewSectionsList.Add(viewSection);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (curve is Line)
                                    {

                                    }  
                                    else
                                    {

                                    }
                                    TaskDialog.Show("Revit", "Фасады в разработке...");
                                    return Result.Cancelled;
                                    //ElevationMarker em = ElevationMarker.CreateElevationMarker(doc, selectedViewFamilyType.Id, origin, 100);
                                    //em.CreateElevation(doc, doc.ActiveView.Id, 0);
                                }
                            }

                            if (selectedViewSheet != null)
                            {
                                List<FamilyInstance> titleBlocksList = new FilteredElementCollector(doc, selectedViewSheet.Id)
                                    .OfCategory(BuiltInCategory.OST_TitleBlocks)
                                    .Cast<FamilyInstance>()
                                    .ToList();

                                XYZ insertPoint = new XYZ(0, 0 - additionalOffset, 0);
                                if (titleBlocksList.Count != 0)
                                {
                                    FamilyInstance titleBlock = titleBlocksList.First();
                                    BoundingBoxXYZ bb = titleBlock.get_BoundingBox(selectedViewSheet);

                                    double minX = bb.Min.X;
                                    double maxY = bb.Max.Y;

                                    insertPoint = new XYZ(minX + 30/304.8, maxY - 20 / 304.8 - additionalOffset, 0);
                                }
                                                                
                                double maxHight = 0;
                                foreach (ViewSection viewSection in viewSectionsList)
                                {
                                    Viewport viewport = Viewport.Create(doc, selectedViewSheet.Id, viewSection.Id, insertPoint);

                                    int viewScale = viewSection.Scale;
                                    BoundingBoxXYZ cropbox = viewSection.CropBox;
                                    XYZ P1 = new XYZ(cropbox.Max.X / viewScale, cropbox.Max.Y / viewScale, 0);
                                    XYZ P2 = new XYZ(cropbox.Min.X / viewScale, cropbox.Min.Y / viewScale, 0);

                                    double deltaX = new XYZ(P1.X, 0, 0).DistanceTo(new XYZ(P2.X, 0, 0)) / 2;
                                    double deltaY = new XYZ(0, P1.Y, 0).DistanceTo(new XYZ(0, P2.Y, 0)) / 2;
                                    ElementTransformUtils.MoveElement(doc, viewport.Id, new XYZ(deltaX, -deltaY, 0));

                                    insertPoint = new XYZ(insertPoint.X + deltaX * 2, insertPoint.Y, insertPoint.Z);
                                    if(deltaY * 2 > maxHight)
                                    {
                                        maxHight = deltaY * 2;
                                    }
                                }
#if R2019 || R2020 || R2021
                                additionalOffset += maxHight + UnitUtils.ConvertToInternalUnits(20, DisplayUnitType.DUT_MILLIMETERS);
#else
                                additionalOffset += maxHight + UnitUtils.ConvertToInternalUnits(20, UnitTypeId.Millimeters);

#endif
                                
                            }
                        }   
                    }
                }
                else
                {
                    WallSelectionFilter wallSelFilter = new WallSelectionFilter();
                    Reference selWall = null;
                    try
                    {
                        selWall = sel.PickObject(ObjectType.Element, wallSelFilter, "Выберите стену!");
                        wall = doc.GetElement(selWall) as Wall;

                        pickedPoint = sel.PickPoint("Укажите сторону размещения");
                    }
                    catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                    {
                        return Result.Cancelled;
                    }

                    if (wall != null)
                    {
                        List<ViewSection> viewSectionsList = new List<ViewSection>();

                        if (selectedUseToBuildName == "rbt_Section")
                        {
                            XYZ wallOrientation = null;
                            if(wall.Flipped)
                            {
                                wallOrientation = wall.Orientation.Negate();
                            }
                            else
                            {
                                wallOrientation = wall.Orientation;
                            }
                            Curve curve = (wall.Location as LocationCurve).Curve;

                            if (curve is Line)
                            {
                                XYZ start = curve.GetEndPoint(0);
                                XYZ end = curve.GetEndPoint(1);

                                XYZ pointH = curve.Project(new XYZ(pickedPoint.X, pickedPoint.Y, start.Z)).XYZPoint;
                                XYZ normalVector = (new XYZ(pickedPoint.X, pickedPoint.Y, start.Z) - pointH).Normalize();

                                if (normalVector.IsAlmostEqualTo(wallOrientation))
                                {
                                    start = curve.GetEndPoint(0);
                                    end = curve.GetEndPoint(1);
                                }
                                else
                                {
                                    start = curve.GetEndPoint(1);
                                    end = curve.GetEndPoint(0);
                                }

                                XYZ curveDir = (end - start).Normalize();
                                double w = (end - start).GetLength();

                                Transform curveTransform = curve.ComputeDerivatives(0.5, true);
                                XYZ origin = curveTransform.Origin;
                                XYZ right = curveDir;
                                XYZ up = XYZ.BasisZ;
                                XYZ viewdir = curveDir.CrossProduct(up).Normalize();

                                BoundingBoxXYZ wallBb = wall.get_BoundingBox(null);
                                double minZ = wallBb.Min.Z;
                                double maxZ = wallBb.Max.Z;

                                Transform transform = Transform.Identity;
                                transform.Origin = origin;
                                transform.BasisX = right;
                                transform.BasisY = up;
                                transform.BasisZ = viewdir;

                                BoundingBoxXYZ sectionBox = new BoundingBoxXYZ();
                                sectionBox.Transform = transform;
                                sectionBox.Min = new XYZ(-w / 2, minZ + indentDown, -indent - wall.Width / 2);
                                sectionBox.Max = new XYZ(w / 2, maxZ + indentUp, projectionDepth - indent - wall.Width / 2);

                                ViewSection viewSection = ViewSection.CreateSection(doc, selectedViewFamilyType.Id, sectionBox);
                                viewSection.Name = $"Р_Ст_{viewSection.Id}";

                                if (useTemplate)
                                {
                                    viewSection.get_Parameter(BuiltInParameter.VIEW_TEMPLATE).Set(viewSectionTemplate.Id);
                                }
                                viewSectionsList.Add(viewSection);
                            }
                            else
                            {
                                XYZ center = (curve as Arc).Center;
                                Curve pickPointLine = Line.CreateBound(center, pickedPoint) as Curve;
                                XYZ start = null;
                                XYZ end = null;
                                if (curve.Intersect(pickPointLine) != SetComparisonResult.Overlap)
                                {
                                    start = curve.GetEndPoint(0) + indent * (center - curve.GetEndPoint(0)).Normalize() + wall.Width / 2 * (center - curve.GetEndPoint(0)).Normalize();
                                    end = curve.GetEndPoint(1) + indent * (center - curve.GetEndPoint(1)).Normalize() + wall.Width / 2 * (center - curve.GetEndPoint(0)).Normalize();
                                }    
                                else
                                {
                                    start = curve.GetEndPoint(0) - indent * (center - curve.GetEndPoint(0)).Normalize() - wall.Width / 2 * (center - curve.GetEndPoint(0)).Normalize();
                                    end = curve.GetEndPoint(1) - indent * (center - curve.GetEndPoint(1)).Normalize() - wall.Width / 2 * (center - curve.GetEndPoint(0)).Normalize();
                                }
                                XYZ arcNormal = (start - center).Normalize();

                                XYZ startVector = (start - center).Normalize();
                                XYZ endVector = (end - center).Normalize();

                                double startEndAngle = endVector.AngleTo(startVector);
                                double anglePerSegment = startEndAngle / curveNumberOfSegments;

                                List<XYZ> arcPoints = new List<XYZ>() { start };
                                if (arcNormal.IsAlmostEqualTo(wallOrientation))
                                {
                                    for (int i = 1; i < curveNumberOfSegments; i++)
                                    {
                                        Transform rotationTransform = Transform.CreateRotationAtPoint(new XYZ(0, 0, 1), -i * anglePerSegment, center);
                                        XYZ rotatedVector = rotationTransform.OfPoint(start);
                                        arcPoints.Add(rotatedVector);
                                    }
                                }
                                else
                                {
                                    for (int i = 1; i < curveNumberOfSegments; i++)
                                    {
                                        Transform rotationTransform = Transform.CreateRotationAtPoint(new XYZ(0, 0, 1), i * anglePerSegment, center);
                                        XYZ rotatedVector = rotationTransform.OfPoint(start);
                                        arcPoints.Add(rotatedVector);
                                    }
                                }
                                arcPoints.Add(end);

                                if (curve.Intersect(pickPointLine) != SetComparisonResult.Overlap)
                                {
                                    arcPoints.Reverse();
                                }
                                if (!arcNormal.IsAlmostEqualTo(wallOrientation))
                                {
                                    arcPoints.Reverse();
                                }

                                for (int i = 0; i < arcPoints.Count - 1; i++)
                                {
                                    XYZ tmpStart = arcPoints[i];
                                    XYZ tmpEnd = arcPoints[i + 1];
                                    XYZ curveDir = (tmpEnd - tmpStart).Normalize();
                                    double w = (tmpEnd - tmpStart).GetLength();

                                    XYZ origin = (tmpStart + tmpEnd) / 2;
                                    XYZ right = curveDir;
                                    XYZ up = XYZ.BasisZ;
                                    XYZ viewdir = curveDir.CrossProduct(up).Normalize();

                                    BoundingBoxXYZ wallBb = wall.get_BoundingBox(null);
                                    double minZ = wallBb.Min.Z;
                                    double maxZ = wallBb.Max.Z;

                                    Transform transform = Transform.Identity;
                                    transform.Origin = origin;
                                    transform.BasisX = right;
                                    transform.BasisY = up;
                                    transform.BasisZ = viewdir;

                                    BoundingBoxXYZ sectionBox = new BoundingBoxXYZ();
                                    sectionBox.Transform = transform;
                                    sectionBox.Min = new XYZ(-w / 2, minZ + indentDown, /*-indent - wall.Width / 2*/ 0);
                                    sectionBox.Max = new XYZ(w / 2, maxZ + indentUp, projectionDepth /*- indent - wall.Width / 2*/);

                                    ViewSection viewSection = ViewSection.CreateSection(doc, selectedViewFamilyType.Id, sectionBox);
                                    viewSection.Name = $"Р_Ст_{viewSection.Id}";

                                    if (useTemplate)
                                    {
                                        viewSection.get_Parameter(BuiltInParameter.VIEW_TEMPLATE).Set(viewSectionTemplate.Id);
                                    }
                                    viewSectionsList.Add(viewSection);
                                }
                            }
                        }
                        else
                        {
                            TaskDialog.Show("Revit", "Фасады в разработке...");
                            return Result.Cancelled;
                            //ElevationMarker em = ElevationMarker.CreateElevationMarker(doc, selectedViewFamilyType.Id, origin, 100);
                            //em.CreateElevation(doc, doc.ActiveView.Id, 0);
                        }

                        if (selectedViewSheet != null)
                        {
                            List<FamilyInstance> titleBlocksList = new FilteredElementCollector(doc, selectedViewSheet.Id)
                                .OfCategory(BuiltInCategory.OST_TitleBlocks)
                                .Cast<FamilyInstance>()
                                .ToList();

                            XYZ insertPoint = new XYZ(0, 0, 0);
                            if (titleBlocksList.Count != 0)
                            {
                                FamilyInstance titleBlock = titleBlocksList.First();
                                BoundingBoxXYZ bb = titleBlock.get_BoundingBox(selectedViewSheet);

                                double minX = bb.Min.X;
                                double maxY = bb.Max.Y;

                                insertPoint = new XYZ(minX + 30 / 304.8, maxY - 20 / 304.8, 0);
                            }

                            double maxHight = 0;
                            foreach (ViewSection viewSection in viewSectionsList)
                            {
                                Viewport viewport = Viewport.Create(doc, selectedViewSheet.Id, viewSection.Id, insertPoint);

                                int viewScale = viewSection.Scale;
                                BoundingBoxXYZ cropbox = viewSection.CropBox;
                                XYZ P1 = new XYZ(cropbox.Max.X / viewScale, cropbox.Max.Y / viewScale, 0);
                                XYZ P2 = new XYZ(cropbox.Min.X / viewScale, cropbox.Min.Y / viewScale, 0);

                                double deltaX = new XYZ(P1.X, 0, 0).DistanceTo(new XYZ(P2.X, 0, 0)) / 2;
                                double deltaY = new XYZ(0, P1.Y, 0).DistanceTo(new XYZ(0, P2.Y, 0)) / 2;
                                ElementTransformUtils.MoveElement(doc, viewport.Id, new XYZ(deltaX, -deltaY, 0));

                                insertPoint = new XYZ(insertPoint.X + deltaX * 2, insertPoint.Y, insertPoint.Z);
                                if (deltaY * 2 > maxHight)
                                {
                                    maxHight = deltaY * 2;
                                }
                            }
                        }
                    }
                }
                t.Commit();
            }
            return Result.Succeeded;
        }
        /// <summary>
        /// Получение списка выбранных через интерфейс помещений
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="sel"></param>
        /// <returns>Список помещений</returns>
        private static List<Room> GetRoomsFromCurrentSelection(Document doc, Selection sel)
        {
            ICollection<ElementId> selectedIds = sel.GetElementIds();
            List<Room> tempRoomsList = new List<Room>();
            foreach (ElementId roomId in selectedIds)
            {
                if (doc.GetElement(roomId) is Room
                    && null != doc.GetElement(roomId).Category
                    && doc.GetElement(roomId).Category.Id.IntegerValue.Equals((int)BuiltInCategory.OST_Rooms))
                {
                    tempRoomsList.Add(doc.GetElement(roomId) as Room);
                }
            }
            tempRoomsList = tempRoomsList.OrderBy(r => r.Number, new AlphanumComparatorFastString()).ThenBy(r => r.Name, new AlphanumComparatorFastString()).ToList();
            return tempRoomsList;
        }
    }
}
